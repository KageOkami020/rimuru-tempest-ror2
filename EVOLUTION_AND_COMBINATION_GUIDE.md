# Rimuru Tempest Mod - Evolution & Ability Combination System

## Overview

This revamp adds two major new systems to the Rimuru mod, inspired by the Shigaraki and Deku mods:

1. **Evolution System** - Rimuru evolves through multiple phases over time
2. **Ability Combination System** - Devoured abilities can be combined into powerful hybrid abilities

## Evolution System

### Phases

Rimuru progresses through 4 evolution phases:

1. **Slime** (Starting phase)
   - Base stats
   - All basic abilities available

2. **Named Monster** (100 evolution points)
   - +15% max health
   - +10% damage
   - +5% movement speed

3. **Demon Lord** (300 evolution points)
   - +35% max health
   - +25% damage
   - +10% movement speed

4. **True Dragon** (600 evolution points)
   - +60% max health
   - +50% damage
   - +15% movement speed

### Gaining Evolution Points

Evolution points are gained through:
- **Time-based**: 1 point per 30 seconds (configurable)
- **Devouring enemies**:
  - Normal enemies: 5 points
  - Elite enemies: 15 points
  - Boss enemies: 30 points

### Configuration

```
[08 - Evolution System]
Enable Evolution System = true
Time Per Evolution Point = 30 seconds
Points Per Devour = 5
```

## Ability Combination System

### How It Works

1. Devour enemies to store their abilities (up to 10 by default)
2. When you have compatible abilities, they can be combined
3. Use the Analyze skill (or a dedicated combination skill) to trigger combinations
4. Combined abilities are more powerful than individual ones

### Storage

- Maximum stored abilities: 10 (configurable)
- Oldest abilities are automatically removed when at capacity
- Each enemy type can only be stored once
- Abilities are consumed when combined

### Combined Abilities

| Combination | Result | Effect |
|-------------|--------|--------|
| Lemurian + Wisp | **Plasma Storm** | Periodic AoE that burns and shocks enemies |
| Lesser Wisp + Jellyfish | **Absolute Zero** | Extreme freeze damage |
| Golem + Beetle Queen | **Titan's Might** | +150 armor and 5% max health regen/sec |
| Imp + Greater Wisp | **Chaos Magic** | Unpredictable magical attacks |
| Elder Lemurian + Golem | **Dragon's Scales** | Fiery armor that damages attackers |
| Nullifier + Scavenger | **Tactical Genius** | 2s cooldown reduction every 3 hits, +5 luck |
| Parent + Mushroom | **Nature's Blessing** | Powerful healing aura |
| Vagrant + Vermin | **Swarm Intelligence** | Orbital attacks that seek enemies |
| Beetle Guard + Bison | **Unstoppable Force** | Devastating charge attacks |
| Clay Dunestrider + Magma Worm | **Volcanic Fury** | Create volcanic devastation zones |

### Using Combinations

**Method 1: Automatic Notification**
- When you devour enemies, you'll receive chat notifications about available combinations
- Compatible abilities are automatically tracked

**Method 2: Manual Combination (TODO)**
- Use a dedicated skill to open the combination interface
- Navigate combinations with Secondary/Utility buttons
- Confirm with Primary attack button

### Configuration

```
[09 - Ability Combination]
Enable Ability Combination = true
Max Stored Abilities = 10
```

## Technical Details

### New Components

- **AbilityStorageController**: Tracks devoured abilities
- **EvolutionController**: Manages evolution progression
- **AbilityCombinationManager**: Handles ability fusion logic
- **Combined Ability Buff Controllers**: Individual controllers for each combined ability

### Integration

The systems integrate seamlessly with existing mechanics:
- Evolution points are gained automatically through Devour
- Stat bonuses are applied during stat recalculation
- Combined abilities use the same buff system as normal devoured abilities
- All systems are fully configurable and can be disabled

## Balancing Notes

### Evolution
- Evolution provides significant but not overwhelming power scaling
- Late-game True Dragon form makes Rimuru feel appropriately powerful
- Time-based progression ensures even passive players evolve
- Active devouring speeds up evolution significantly

### Ability Combinations
- Combined abilities trade versatility for power
- Consuming abilities to combine prevents ability hoarding
- 10 ability storage limit forces strategic choices
- Combined abilities last the same duration as normal buffs

## Future Enhancements

Potential future additions:
- UI indicators for evolution progress
- Visual effects for evolution phases (model changes, particles)
- More combined ability recipes
- Phase-specific exclusive abilities
- Skill to manually trigger combinations
- Combination preview before consuming abilities
