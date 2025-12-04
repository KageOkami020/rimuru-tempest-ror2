/*******************************************************************************
 * Elevator Control System for TM4C123GH6PM Microcontroller
 * 
 * This embedded C program implements a 4-floor elevator control system using:
 *   - A stepper motor for elevator movement (controlled via finite state machine)
 *   - A servo motor for door operation
 *   - A buzzer for audio feedback
 *   - Floor sensors and button inputs
 *   - LED indicators for current/target floor display
 * 
 * Hardware Connections:
 *   - PB0-PB3: Stepper motor coil outputs (half-step sequence)
 *   - PB7: Servo motor PWM signal (software PWM)
 *   - PE0-PE3: Floor sensors (active high inputs with pull-downs)
 *   - PE4: Buzzer output (software tone generation)
 *   - PA4-PA7: Floor selection buttons (active low with pull-ups)
 *   - PC4-PC7: LED indicators for target floor display
 *   - PD0-PD1: Motor direction control signals
 * 
 * System Clock: 16 MHz internal oscillator
 ******************************************************************************/

#include "tm4c123gh6pm.h"    /* TM4C123GH6PM register definitions */
#include <stdint.h>          /* Standard integer types (uint8_t, uint32_t, etc.) */

#define SYSCLK 16000000UL    /* System clock frequency: 16 MHz */

/*******************************************************************************
 * Function Prototypes
 ******************************************************************************/
void Ports_Init(void);                        /* Initialize all GPIO ports */
void SysTick_Init(void);                      /* Initialize SysTick timer with interrupt */
void delay_us(int us);                        /* Microsecond delay (unused in current implementation) */
void delay_ms(int ms);                        /* Millisecond delay using SysTick counter */
void motor_dir(unsigned volatile char input); /* Update stepper motor state based on direction */
void Buzzer_Tone(int freq_hz, int duration_ms); /* Play a tone on the buzzer */
void Servo_Init_GPIO(void);                   /* Initialize servo GPIO (handled in Ports_Init) */
void Servo_Write(int angle);                  /* Set servo position (0-180 degrees) */

/*******************************************************************************
 * Finite State Machine for Stepper Motor Control (Half-Step Sequence)
 * 
 * The stepper motor uses a half-step sequence for smoother operation.
 * Each state outputs a 4-bit pattern to PB0-PB3 controlling the motor coils.
 * 
 * Half-step sequence provides 8 steps per electrical cycle:
 *   Step 1: 1000 (0x08) - Coil A
 *   Step 2: 1100 (0x0C) - Coils A+B
 *   Step 3: 0100 (0x04) - Coil B
 *   Step 4: 0110 (0x06) - Coils B+C
 *   Step 5: 0010 (0x02) - Coil C
 *   Step 6: 0011 (0x03) - Coils C+D
 *   Step 7: 0001 (0x01) - Coil D
 *   Step 8: 1001 (0x09) - Coils D+A
 * 
 * Next state transitions:
 *   Next[0] = STOP (stay in current state)
 *   Next[1] = CW (clockwise - advance to next state)
 *   Next[2] = CCW (counter-clockwise - go to previous state)
 ******************************************************************************/
struct State {
    unsigned char Out;           /* Output pattern for PB3-0 (motor coils) */
    const struct State *Next[3]; /* Next state pointers: [STOP, CW, CCW] */
};
typedef const struct State STyp;

/* State table defining the 8-step half-step sequence */
STyp fsm[8] = {
    {0x08, {&fsm[0], &fsm[1], &fsm[7]}}, /* Step 1: A energized; CW->Step2, CCW->Step8 */
    {0x0C, {&fsm[1], &fsm[2], &fsm[0]}}, /* Step 2: A+B energized; CW->Step3, CCW->Step1 */
    {0x04, {&fsm[2], &fsm[3], &fsm[1]}}, /* Step 3: B energized; CW->Step4, CCW->Step2 */
    {0x06, {&fsm[3], &fsm[4], &fsm[2]}}, /* Step 4: B+C energized; CW->Step5, CCW->Step3 */
    {0x02, {&fsm[4], &fsm[5], &fsm[3]}}, /* Step 5: C energized; CW->Step6, CCW->Step4 */
    {0x03, {&fsm[5], &fsm[6], &fsm[4]}}, /* Step 6: C+D energized; CW->Step7, CCW->Step5 */
    {0x01, {&fsm[6], &fsm[7], &fsm[5]}}, /* Step 7: D energized; CW->Step8, CCW->Step6 */
    {0x09, {&fsm[7], &fsm[0], &fsm[6]}}  /* Step 8: D+A energized; CW->Step1, CCW->Step7 */
};
STyp *Pt = &fsm[0]; /* Current state pointer, initialized to Step 1 */

/*******************************************************************************
 * Global State Variables
 ******************************************************************************/
volatile unsigned char current_floor = 0;  /* Current floor sensor reading (PE0-PE3) */
volatile unsigned char current_input = 0;  /* Current button input state (PA4-PA7 inverted) */
volatile unsigned char target_floor = 0;   /* Requested destination floor */
volatile unsigned char last_floor = 0;     /* Last valid floor position detected */
volatile unsigned char flag = 0;           /* State flag: 0=idle/ready, 1=moving to target */
volatile unsigned char dir = 0;            /* Motor direction: 0=stop, 1=CW, 2=CCW */

/*******************************************************************************
 * Timing Variables (updated by SysTick interrupt every 0.5ms)
 ******************************************************************************/
volatile uint32_t systick_ticks_05ms = 0;  /* Counter incremented every 0.5ms tick */
volatile uint32_t systick_millis = 0;      /* Millisecond counter (for delay_ms) */

/*******************************************************************************
 * Servo Control Variables (software PWM via SysTick interrupt)
 * 
 * Servo timing uses 0.5ms resolution:
 *   - 20ms period = 40 ticks of 0.5ms
 *   - 1.0ms pulse (0°) = 2 ticks
 *   - 1.5ms pulse (90°) = 3 ticks (center/default)
 *   - 2.0ms pulse (180°) = 4 ticks
 ******************************************************************************/
volatile uint8_t servo_pulse_ticks = 3;    /* Pulse width in 0.5ms ticks (default: 1.5ms = center) */
const uint8_t servo_period_ticks = 40;     /* PWM period: 20ms / 0.5ms = 40 ticks */
volatile uint8_t servo_phase = 0;          /* Current position in PWM cycle (0..39) */

/*******************************************************************************
 * Buzzer Control Variables (software tone generation via SysTick interrupt)
 * 
 * The buzzer generates tones by toggling PE4 at a specific frequency.
 * Resolution is limited to 0.5ms, so maximum frequency is 1000Hz.
 ******************************************************************************/
volatile uint32_t buzzer_half_period_ticks = 0; /* Half-period in 0.5ms ticks; 0=buzzer off */
volatile uint32_t buzzer_remaining_cycles = 0;  /* Remaining toggle cycles (0=continuous) */
volatile uint8_t buzzer_state = 0;              /* Current output state: 0=low, 1=high */
volatile uint32_t buzzer_counter = 0;           /* Tick counter for timing toggles */

unsigned long buzzer_duty = 6400; /* Legacy variable (unused with software buzzer) */

/*******************************************************************************
 * MAIN FUNCTION
 * 
 * Implements the elevator control loop:
 * 1. Initialize all hardware peripherals
 * 2. Read floor sensors and button inputs
 * 3. Determine travel direction based on current and target floors
 * 4. Drive stepper motor in appropriate direction
 * 5. When target floor reached, stop motor and operate door servo
 * 6. Sound buzzer notification
 ******************************************************************************/
int main(void)
{
    /* Initialize all GPIO ports and peripherals */
    Ports_Init();
    SysTick_Init();
   
    /* Initialize buzzer to off state (PE4 configured in Ports_Init) */
    Buzzer_Tone(0, 0);

    /* Ensure motor direction outputs are cleared (stopped state) */
    GPIO_PORTD_DATA_R &= ~0x03U;
    flag = 0;
    
    /* Initialize servo to closed position (0 degrees) */
    Servo_Write(0);
    
    /***************************************************************************
     * Main Control Loop
     * Continuously monitors sensors and buttons, controls elevator movement
     ***************************************************************************/
    while(1) {
        /* Read floor sensor inputs from PE0-PE3 (active high)
         * Each bit represents a floor: 0x01=Floor1, 0x02=Floor2, etc. */
        current_floor = (unsigned char)(GPIO_PORTE_DATA_R & 0x0FU);
        
        /* Read button inputs from PA4-PA7 (active low, so invert and shift)
         * Each bit represents a floor button: 0x01=Floor1, 0x02=Floor2, etc. */
        current_input = (unsigned char)((~GPIO_PORTA_DATA_R & 0xF0U) >> 4);
                  
        /* Update last_floor only when a valid floor sensor is detected
         * This tracks the actual elevator position */
        if ((current_floor != 0x00U) && (current_floor != last_floor)) {
            last_floor = current_floor;
        }

        /* Accept new target floor when:
         * - Not currently moving to a target (flag == 0)
         * - A button is pressed (current_input != 0)
         * - Pressed button differs from current target */
        if ((flag == 0) && (current_input != 0x00U) && (current_input != target_floor)) {
            target_floor = current_input;
            flag = 1; /* Set flag to indicate movement in progress */
        }

        /* Determine motor direction when target differs from current position
         * Direction logic:
         *   - target_floor < last_floor: Elevator needs to go UP (CCW rotation)
         *   - target_floor > last_floor: Elevator needs to go DOWN (CW rotation)
         *   - target_floor == last_floor: Stop */
        if ((target_floor != last_floor) && (last_floor != 0x00U) && (target_floor != 0x00U)) {
            if (target_floor < last_floor) {
                dir = 2; /* CCW = going up (lower floor number = higher position) */
            } else if (target_floor > last_floor) {
                dir = 1; /* CW = going down (higher floor number = lower position) */
            } else {
                dir = 0; /* Stop */
            }
            motor_dir(dir); /* Apply motor direction */
        }

        /* Update LED display with current target floor
         * LEDs on PC4-PC7 show which floor button was pressed */
        GPIO_PORTC_DATA_R = 0x00U;                          /* Clear all LEDs */
        GPIO_PORTC_DATA_R |= (unsigned int)(target_floor << 4); /* Set target floor LEDs */

        /* Arrival detection: Check if elevator has reached target floor
         * Conditions:
         *   - last_floor matches target_floor (at destination)
         *   - Both values are valid (non-zero)
         *   - Movement was in progress (flag == 1) */
        if ((last_floor == target_floor) && (last_floor != 0x00U) && (target_floor != 0x00U) && (flag == 1)) {
            dir = 0;          /* Stop motor */
            motor_dir(dir);
            flag = 0;         /* Reset movement flag */
            
            /* Open door: Move servo to 180 degrees (fully open) */
            Servo_Write(180);
            delay_ms(1000);   /* Wait for servo to complete rotation */

            /* Door open/close cycle with buzzer notification
             * Repeats 3 times: buzzer beep, pause, close door, reopen */
            unsigned char i;
            for (i = 0; i < 3; i++) {
                GPIO_PORTC_DATA_R = 0x00U; /* Turn off LEDs during beep */
                
                /* Sound buzzer: 500Hz tone for 100ms */
                Buzzer_Tone(500U, 100U);
                delay_ms(500); /* Wait for tone and additional pause */
                
                /* Restore LED display and close door */
                GPIO_PORTC_DATA_R |= (uint32_t)(target_floor << 4);
                Servo_Write(0);    /* Close door (0 degrees) */
                delay_ms(1000);    /* Wait for door to close */
            }
        }
    }
}

/*******************************************************************************
 * Ports_Init - Initialize All GPIO Ports
 * 
 * Configures GPIO pins for the elevator control system:
 *   - Port A: Button inputs (PA4-PA7)
 *   - Port B: Stepper motor outputs (PB0-PB3) and servo signal (PB7)
 *   - Port C: LED outputs (PC4-PC7)
 *   - Port D: Motor direction control (PD0-PD1)
 *   - Port E: Floor sensors (PE0-PE3) and buzzer output (PE4)
 ******************************************************************************/
void Ports_Init(void)
{
    /* Enable clock to GPIO Ports A through F (bits 0-5 of RCGCGPIO) */
    SYSCTL_RCGCGPIO_R |= 0x3FU;
    
    /* Wait for GPIO module clocks to stabilize */
    while ((SYSCTL_PRGPIO_R & 0x3FU) == 0U) { }

    /***************************************************************************
     * Port B Configuration: Stepper Motor (PB0-PB3) and Servo (PB7)
     ***************************************************************************/
    GPIO_PORTB_DATA_R &= ~0x0FU;   /* Clear motor outputs initially */
    GPIO_PORTB_DIR_R  |= 0x0FU;    /* PB0-PB3 as outputs */
    GPIO_PORTB_DEN_R  |= 0x0FU;    /* Enable digital function */
    GPIO_PORTB_AFSEL_R &= ~0x0FU;  /* Use GPIO function (not alternate) */
    GPIO_PORTB_AMSEL_R &= ~0x0FU;  /* Disable analog mode */

    /***************************************************************************
     * Port E Configuration: Floor Sensors (PE0-PE3) and Buzzer (PE4)
     ***************************************************************************/
    GPIO_PORTE_DATA_R &= ~0x0FU;   /* Clear data register */
    GPIO_PORTE_DIR_R  &= ~0x0FU;   /* PE0-PE3 as inputs (floor sensors) */
    GPIO_PORTE_DEN_R  |= 0x0FU;    /* Enable digital function */
    GPIO_PORTE_AFSEL_R &= ~0x0FU;  /* Use GPIO function */
    GPIO_PORTE_AMSEL_R &= ~0x0FU;  /* Disable analog mode */
    GPIO_PORTE_PDR_R   |= 0x0FU;   /* Enable internal pull-down resistors */

    /***************************************************************************
     * Port C Configuration: LED Indicators (PC4-PC7)
     ***************************************************************************/
    GPIO_PORTC_DATA_R &= ~0xF0U;   /* Clear LED outputs */
    GPIO_PORTC_DIR_R  |= 0xF0U;    /* PC4-PC7 as outputs */
    GPIO_PORTC_DEN_R  |= 0xF0U;    /* Enable digital function */
    GPIO_PORTC_AFSEL_R &= ~0xF0U;  /* Use GPIO function */
    GPIO_PORTC_AMSEL_R &= ~0xF0U;  /* Disable analog mode */

    /***************************************************************************
     * Port A Configuration: Floor Selection Buttons (PA4-PA7)
     * Buttons are active-low with internal pull-up resistors
     ***************************************************************************/
    GPIO_PORTA_DIR_R  &= ~0xF0U;   /* PA4-PA7 as inputs */
    GPIO_PORTA_DEN_R  |= 0xF0U;    /* Enable digital function */
    GPIO_PORTA_AFSEL_R &= ~0xF0U;  /* Use GPIO function */
    GPIO_PORTA_PUR_R  |= 0xF0U;    /* Enable internal pull-up resistors */

    /***************************************************************************
     * Port D Configuration: Motor Direction Control (PD0-PD1)
     * PD0 and PD1 control external motor driver direction inputs
     * Other pins configured as inputs with pull-ups
     ***************************************************************************/
    GPIO_PORTD_LOCK_R = 0x4C4F434BU;   /* Unlock Port D (magic number) */
    GPIO_PORTD_CR_R   |= 0xFFU;        /* Allow changes to all PD pins */
    GPIO_PORTD_DEN_R  |= 0xFFU;        /* Enable digital function for all pins */
    GPIO_PORTD_AFSEL_R &= ~0xFFU;      /* Use GPIO function */
    GPIO_PORTD_PUR_R  |= 0xFCU;        /* Pull-ups on PD2-7 (inputs) */
    GPIO_PORTD_AMSEL_R &= ~0xFFU;      /* Disable analog mode */

    GPIO_PORTD_DIR_R &= ~0xFFU;        /* Default all as inputs */
    GPIO_PORTD_DIR_R |= 0x03U;         /* PD0, PD1 as outputs */
    GPIO_PORTD_DATA_R &= ~0x03U;       /* Clear direction outputs (stopped) */

    /***************************************************************************
     * PB7 Configuration: Servo Signal Output (Software PWM)
     * Configured as GPIO output for bit-banged PWM signal
     ***************************************************************************/
    GPIO_PORTB_AFSEL_R &= ~0x80U;  /* Ensure GPIO function (not alternate) */
    GPIO_PORTB_DEN_R   |=  0x80U;  /* Enable digital function */
    GPIO_PORTB_DIR_R   |=  0x80U;  /* PB7 as output */
    GPIO_PORTB_DATA_R  &= ~0x80U;  /* Initialize low */

    /***************************************************************************
     * PE4 Configuration: Buzzer Signal Output (Software Tone Generation)
     * Configured as GPIO output for bit-banged square wave
     ***************************************************************************/
    GPIO_PORTE_AFSEL_R &= ~0x10U;  /* Ensure GPIO function */
    GPIO_PORTE_DEN_R   |=  0x10U;  /* Enable digital function */
    GPIO_PORTE_DIR_R   |=  0x10U;  /* PE4 as output */
    GPIO_PORTE_DATA_R  &= ~0x10U;  /* Initialize low (buzzer off) */
}

/*******************************************************************************
 * SysTick_Init - Initialize SysTick Timer
 * 
 * Configures SysTick to generate interrupts every 0.5ms (2000 Hz).
 * The interrupt handler updates timing counters and generates:
 *   - Software PWM for servo motor
 *   - Software square wave for buzzer
 * 
 * Reload value calculation: (16MHz / 2000Hz) - 1 = 7999
 ******************************************************************************/
void SysTick_Init(void)
{
    /* Calculate reload value for 0.5ms period (2000 Hz tick rate) */
    uint32_t reload = (SYSCLK / 2000U) - 1U;
    
    NVIC_ST_RELOAD_R = reload;    /* Set reload value */
    NVIC_ST_CURRENT_R = 0U;       /* Clear current value (any write clears) */
    
    /* Enable SysTick with system clock and interrupt enabled
     * Bit 0: Enable, Bit 1: Interrupt enable, Bit 2: Use core clock */
    NVIC_ST_CTRL_R = 0x07U;
}

/*******************************************************************************
 * SysTick_Handler - SysTick Interrupt Service Routine
 * 
 * Called every 0.5ms to handle:
 * 1. Time tracking (0.5ms and 1ms counters)
 * 2. Servo PWM signal generation (20ms period)
 * 3. Buzzer tone generation (variable frequency)
 ******************************************************************************/
void SysTick_Handler(void)
{
    /* Increment 0.5ms tick counter */
    systick_ticks_05ms++;

    /* Update millisecond counter every 2 ticks (every 1ms) */
    if ((systick_ticks_05ms & 1U) == 0U) {
        systick_millis++;
    }

    /***************************************************************************
     * Servo PWM Generation
     * 
     * Creates a 50Hz (20ms period) PWM signal on PB7.
     * Pulse width controlled by servo_pulse_ticks:
     *   - 2 ticks = 1.0ms = 0 degrees
     *   - 3 ticks = 1.5ms = 90 degrees
     *   - 4 ticks = 2.0ms = 180 degrees
     ***************************************************************************/
    servo_phase++;
    if (servo_phase >= servo_period_ticks) {
        servo_phase = 0; /* Reset phase counter at end of 20ms period */
    }

    if (servo_phase == 0) {
        /* Start of PWM frame: Set servo signal HIGH */
        GPIO_PORTB_DATA_R |= 0x80U; /* PB7 = 1 */
    } else if (servo_phase == servo_pulse_ticks) {
        /* End of pulse: Set servo signal LOW */
        GPIO_PORTB_DATA_R &= ~0x80U; /* PB7 = 0 */
    }

    /***************************************************************************
     * Buzzer Tone Generation
     * 
     * Toggles PE4 at the specified frequency to generate a square wave.
     * buzzer_half_period_ticks determines the toggle interval.
     * buzzer_remaining_cycles counts down to stop the tone after duration.
     ***************************************************************************/
    if (buzzer_half_period_ticks) {
        buzzer_counter++;
        
        /* Toggle when half-period has elapsed */
        if (buzzer_counter >= buzzer_half_period_ticks) {
            buzzer_counter = 0;
            
            /* Toggle PE4 output */
            if (buzzer_state) {
                GPIO_PORTE_DATA_R &= ~0x10U; /* PE4 = 0 */
                buzzer_state = 0;
            } else {
                GPIO_PORTE_DATA_R |= 0x10U;  /* PE4 = 1 */
                buzzer_state = 1;
            }
            
            /* Decrement remaining cycles if finite duration */
            if (buzzer_remaining_cycles) {
                buzzer_remaining_cycles--;
                if (buzzer_remaining_cycles == 0) {
                    /* Tone complete: Turn off buzzer */
                    buzzer_half_period_ticks = 0;
                    buzzer_state = 0;
                    GPIO_PORTE_DATA_R &= ~0x10U; /* Ensure PE4 = 0 */
                }
            }
        }
    }
}

/*******************************************************************************
 * delay_ms - Blocking Millisecond Delay
 * 
 * Waits for the specified number of milliseconds using the SysTick-based
 * millisecond counter. Uses busy-wait polling of systick_millis variable
 * which is updated by the SysTick interrupt handler.
 * 
 * Parameters:
 *   ms - Number of milliseconds to wait
 * 
 * Note: Resolution is 1ms. Actual delay may vary by up to 1ms due to
 *       synchronization with the SysTick interrupt.
 ******************************************************************************/
void delay_ms(int ms)
{
    uint32_t target = systick_millis + ms; /* Calculate target tick count */
    while (systick_millis != target) { }   /* Busy wait until target reached */
}

/*******************************************************************************
 * motor_dir - Update Stepper Motor Direction and Step
 * 
 * Outputs the current state's coil pattern to the motor driver and advances
 * the finite state machine to the next state based on direction input.
 * 
 * Parameters:
 *   input - Direction command:
 *           0 = Stop (stay in current state)
 *           1 = Clockwise (advance to next state)
 *           2 = Counter-clockwise (return to previous state)
 * 
 * This function should be called repeatedly in a loop for continuous rotation.
 * The 2ms delay between calls determines the motor stepping speed.
 ******************************************************************************/
void motor_dir(unsigned volatile char input)
{
    /* Output current state pattern to motor driver (PB0-PB3) */
    GPIO_PORTB_DATA_R = Pt->Out;
    
    /* Wait for motor coils to energize */
    delay_ms(2);
    
    /* Advance to next state based on direction (mask to 0..3 for safety) */
    Pt = Pt->Next[input & 0x03U];
}

/*******************************************************************************
 * Buzzer_Tone - Generate a Tone on the Buzzer
 * 
 * Configures the SysTick interrupt handler to generate a square wave on PE4
 * at the specified frequency for the specified duration. The tone is generated
 * asynchronously in the interrupt handler.
 * 
 * Parameters:
 *   freq_hz     - Tone frequency in Hz (0 to disable)
 *   duration_ms - Tone duration in milliseconds (0 for continuous until stopped)
 * 
 * Note: Due to 0.5ms resolution, maximum achievable frequency is approximately
 *       1000Hz. Higher frequencies will be generated at reduced accuracy.
 * 
 * Example: Buzzer_Tone(500, 100) generates a 500Hz tone for 100ms
 ******************************************************************************/
void Buzzer_Tone(int freq_hz, int duration_ms)
{
    /* If frequency or duration is zero, turn off buzzer */
    if (freq_hz == 0 || duration_ms == 0) {
        buzzer_half_period_ticks = 0;
        buzzer_remaining_cycles = 0;
        buzzer_state = 0;
        GPIO_PORTE_DATA_R &= ~0x10U; /* Ensure PE4 is low */
        return;
    }
    
    /* Calculate half-period in microseconds
     * Full period = 1/freq_hz seconds = 1000000/freq_hz microseconds
     * Half period = 500000/freq_hz microseconds */
    long half_period_us = 1000000UL / (freq_hz * 2UL);
    
    /* Convert microseconds to 0.5ms ticks with rounding
     * ticks = (microseconds + 250) / 500 */
    long half_ticks = (half_period_us + 250UL) / 500UL;
    
    /* Ensure minimum of 1 tick (0.5ms minimum half-period) */
    if (half_ticks == 0) half_ticks = 1;
    buzzer_half_period_ticks = half_ticks;
    
    /* Calculate number of toggle cycles for the specified duration
     * Each complete wave cycle requires 2 toggles (high->low->high)
     * cycles = (duration_ms * 1000us) / half_period_us */
    buzzer_remaining_cycles = (duration_ms * 1000UL) / (half_period_us == 0 ? 1 : half_period_us);
    
    /* Reset counters */
    buzzer_counter = 0;
    buzzer_state = 0;
}

/*******************************************************************************
 * Servo_Write - Set Servo Motor Position
 * 
 * Configures the servo pulse width to move the servo to the specified angle.
 * The servo is controlled via software PWM generated in the SysTick interrupt.
 * 
 * Standard servo timing:
 *   - 1.0ms pulse width = 0 degrees (minimum position)
 *   - 1.5ms pulse width = 90 degrees (center position)
 *   - 2.0ms pulse width = 180 degrees (maximum position)
 * 
 * Parameters:
 *   angle - Desired servo position in degrees (0 to 180)
 *           Values outside this range are clamped.
 * 
 * Note: Due to 0.5ms resolution (SysTick tick period), only three distinct
 *       positions are achievable:
 *         - 0-44 degrees maps to 2 ticks (1.0ms)
 *         - 45-134 degrees maps to 3 ticks (1.5ms)
 *         - 135-180 degrees maps to 4 ticks (2.0ms)
 ******************************************************************************/
void Servo_Write(int angle)
{
    /* Clamp angle to valid range 0-180 degrees */
    if (angle < 0) angle = 0;
    if (angle > 180) angle = 180;
    
    /* Calculate pulse width in microseconds
     * Linear interpolation: 0° = 1000us, 180° = 2000us
     * pulse_us = 1000 + (angle * 1000 / 180) */
    int pulse_us = 1000U + (int)angle * 1000U / 180U;
    
    /* Convert microseconds to 0.5ms ticks with rounding
     * ticks = (pulse_us + 250) / 500
     * 1000us -> 2 ticks, 1500us -> 3 ticks, 2000us -> 4 ticks */
    char ticks = (char)((pulse_us + 250U) / 500U);
    
    /* Clamp to valid range: 2-4 ticks (1.0ms to 2.0ms) */
    if (ticks < 2) ticks = 2;   /* Minimum: 1.0ms pulse */
    if (ticks > 4) ticks = 4;   /* Maximum: 2.0ms pulse */
    
    /* Update global variable (read by SysTick interrupt handler) */
    servo_pulse_ticks = ticks;
}
