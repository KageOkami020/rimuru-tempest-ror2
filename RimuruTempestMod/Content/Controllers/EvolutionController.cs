using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.Controllers
{
    /// <summary>
    /// Manages Rimuru's evolution phases - inspired by Deku mod's progression system
    /// Character evolves from Slime -> Named Monster -> Demon Lord -> True Dragon
    /// </summary>
    public class EvolutionController : MonoBehaviour
    {
        public CharacterMaster characterMaster;
        public CharacterBody characterBody;

        // Evolution phases
        public enum EvolutionPhase
        {
            Slime = 0,           // Starting phase
            NamedMonster = 1,    // After devouring enough enemies
            DemonLord = 2,       // Mid-game evolution
            TrueDragon = 3       // Final form
        }

        public EvolutionPhase currentPhase = EvolutionPhase.Slime;
        
        // Experience points for evolution
        public float evolutionPoints = 0f;
        
        // Points required for each phase
        public float[] phaseRequirements = new float[]
        {
            0f,      // Slime (starting)
            100f,    // Named Monster
            300f,    // Demon Lord  
            600f     // True Dragon
        };

        // Stat multipliers per phase
        public float[] healthMultipliers = new float[] { 1.0f, 1.15f, 1.35f, 1.6f };
        public float[] damageMultipliers = new float[] { 1.0f, 1.1f, 1.25f, 1.5f };
        public float[] speedMultipliers = new float[] { 1.0f, 1.05f, 1.1f, 1.15f };

        // Time-based evolution
        private float timeInRun = 0f;
        public bool enableTimeBasedEvolution = true;
        public float timePerEvolutionPoint = 30f; // Gain 1 point per 30 seconds

        // Devour-based evolution
        public float pointsPerDevour = 5f;
        public float pointsPerEliteDevour = 15f;
        public float pointsPerBossDevour = 30f;

        // Evolution notification cooldown
        private float notificationCooldown = 0f;
        private const float NOTIFICATION_COOLDOWN_TIME = 5f;

        public void Awake()
        {
            characterMaster = gameObject.GetComponent<CharacterMaster>();
        }

        public void Start()
        {
            if (characterMaster)
            {
                characterBody = characterMaster.GetBody();
            }
            
            // Start at Slime phase
            currentPhase = EvolutionPhase.Slime;
            evolutionPoints = 0f;
            timeInRun = 0f;
        }

        public void FixedUpdate()
        {
            if (!characterBody && characterMaster)
            {
                characterBody = characterMaster.GetBody();
            }

            if (characterBody)
            {
                // Time-based evolution progression
                if (enableTimeBasedEvolution && Config.enableEvolutionSystem.Value)
                {
                    timeInRun += Time.fixedDeltaTime;
                    
                    // Add evolution points based on time
                    if (timeInRun >= timePerEvolutionPoint)
                    {
                        timeInRun -= timePerEvolutionPoint;
                        AddEvolutionPoints(1f, false);
                    }
                }

                // Check for phase upgrades
                CheckPhaseUpgrade();

                // Update notification cooldown
                if (notificationCooldown > 0f)
                {
                    notificationCooldown -= Time.fixedDeltaTime;
                }
            }
        }

        /// <summary>
        /// Adds evolution points and checks for phase upgrade
        /// </summary>
        public void AddEvolutionPoints(float points, bool showNotification = true)
        {
            if (!Config.enableEvolutionSystem.Value)
            {
                return;
            }

            evolutionPoints += points;

            if (showNotification && notificationCooldown <= 0f)
            {
                ShowEvolutionProgress();
                notificationCooldown = NOTIFICATION_COOLDOWN_TIME;
            }

            CheckPhaseUpgrade();
        }

        /// <summary>
        /// Called when devouring an enemy
        /// </summary>
        public void OnEnemyDevoured(CharacterBody victimBody)
        {
            if (!Config.enableEvolutionSystem.Value)
            {
                return;
            }

            float points = pointsPerDevour;

            // Bonus points for elite enemies
            if (victimBody.isElite)
            {
                points = pointsPerEliteDevour;
            }

            // Bonus points for bosses
            if (victimBody.isBoss)
            {
                points = pointsPerBossDevour;
            }

            AddEvolutionPoints(points, true);
        }

        /// <summary>
        /// Checks if enough points to upgrade to next phase
        /// </summary>
        private void CheckPhaseUpgrade()
        {
            EvolutionPhase nextPhase = currentPhase + 1;

            // Check if can evolve to next phase
            if ((int)nextPhase < phaseRequirements.Length && 
                evolutionPoints >= phaseRequirements[(int)nextPhase])
            {
                EvolveToPhase(nextPhase);
            }
        }

        /// <summary>
        /// Evolves to a new phase
        /// </summary>
        private void EvolveToPhase(EvolutionPhase newPhase)
        {
            if (newPhase <= currentPhase || (int)newPhase >= phaseRequirements.Length)
            {
                return;
            }

            EvolutionPhase oldPhase = currentPhase;
            currentPhase = newPhase;

            // Show evolution notification
            ShowEvolutionNotification(oldPhase, newPhase);

            // Apply phase-specific effects
            ApplyPhaseEffects();

            // Play evolution effect
            PlayEvolutionEffect();

            // Recalculate stats
            if (characterBody)
            {
                characterBody.RecalculateStats();
            }
        }

        /// <summary>
        /// Shows evolution notification to player
        /// </summary>
        private void ShowEvolutionNotification(EvolutionPhase oldPhase, EvolutionPhase newPhase)
        {
            string phaseName = GetPhaseName(newPhase);
            Chat.AddMessage($"<style=cIsUtility>═══════════════════════════</style>");
            Chat.AddMessage($"<style=cDeath>EVOLUTION COMPLETE!</style>");
            Chat.AddMessage($"<style=cIsUtility>{GetPhaseName(oldPhase)} → {phaseName}</style>");
            Chat.AddMessage($"<style=cIsUtility>New abilities unlocked!</style>");
            Chat.AddMessage($"<style=cIsUtility>═══════════════════════════</style>");
        }

        /// <summary>
        /// Shows current evolution progress
        /// </summary>
        private void ShowEvolutionProgress()
        {
            EvolutionPhase nextPhase = currentPhase + 1;
            if ((int)nextPhase < phaseRequirements.Length)
            {
                float progress = (evolutionPoints - phaseRequirements[(int)currentPhase]) / 
                                (phaseRequirements[(int)nextPhase] - phaseRequirements[(int)currentPhase]) * 100f;
                
                Chat.AddMessage($"<style=cIsUtility>Evolution Progress: {progress:F0}% to {GetPhaseName(nextPhase)}</style>");
            }
        }

        /// <summary>
        /// Gets display name for a phase
        /// </summary>
        private string GetPhaseName(EvolutionPhase phase)
        {
            switch (phase)
            {
                case EvolutionPhase.Slime:
                    return "Slime";
                case EvolutionPhase.NamedMonster:
                    return "Named Monster";
                case EvolutionPhase.DemonLord:
                    return "Demon Lord";
                case EvolutionPhase.TrueDragon:
                    return "True Dragon";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Applies buffs and effects for current phase
        /// </summary>
        private void ApplyPhaseEffects()
        {
            if (!characterBody)
            {
                return;
            }

            // Apply phase-specific buffs
            switch (currentPhase)
            {
                case EvolutionPhase.NamedMonster:
                    characterBody.ApplyBuff(Buffs.evolutionPhase1Buff.buffIndex);
                    break;
                case EvolutionPhase.DemonLord:
                    characterBody.ApplyBuff(Buffs.evolutionPhase2Buff.buffIndex);
                    break;
                case EvolutionPhase.TrueDragon:
                    characterBody.ApplyBuff(Buffs.evolutionPhase3Buff.buffIndex);
                    break;
            }
        }

        /// <summary>
        /// Plays visual/audio effect for evolution
        /// </summary>
        private void PlayEvolutionEffect()
        {
            if (!characterBody)
            {
                return;
            }

            // Play sound effect
            AkSoundEngine.PostEvent("RimuruAnalyse", characterBody.gameObject);

            // Spawn visual effect
            EffectManager.SpawnEffect(AssetsRimuru.devourskillgetEffect, new EffectData
            {
                origin = characterBody.corePosition,
                scale = 3f,
                rotation = Quaternion.identity
            }, true);
        }

        /// <summary>
        /// Gets stat multiplier for current phase
        /// </summary>
        public float GetHealthMultiplier()
        {
            return healthMultipliers[(int)currentPhase];
        }

        public float GetDamageMultiplier()
        {
            return damageMultipliers[(int)currentPhase];
        }

        public float GetSpeedMultiplier()
        {
            return speedMultipliers[(int)currentPhase];
        }

        /// <summary>
        /// Checks if an ability is unlocked at current phase
        /// </summary>
        public bool IsAbilityUnlocked(string abilityName)
        {
            // Basic abilities available at Slime phase
            if (currentPhase >= EvolutionPhase.Slime)
            {
                return true;
            }

            // Additional logic can be added for phase-specific abilities
            return false;
        }
    }
}
