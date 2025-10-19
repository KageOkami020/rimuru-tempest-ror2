using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.Controllers
{
    /// <summary>
    /// Manages Rimuru's evolution phases based on player level
    /// Level 1-9: Pure Slime
    /// Level 10-19: Human Form Acquired (Slime + Human)
    /// Level 20-29: Demon Lord Awakening
    /// Level 30+: True Dragon Ascension
    /// </summary>
    public class EvolutionController : MonoBehaviour
    {
        public CharacterMaster characterMaster;
        public CharacterBody characterBody;

        // Evolution phases based on level
        public enum EvolutionPhase
        {
            PureSlime = 0,        // Level 1-9: Slime only
            HumanFormAcquired = 1, // Level 10-19: Slime + Human toggle
            DemonLordAwakening = 2, // Level 20-29: Enhanced forms
            TrueDragonAscension = 3 // Level 30+: Mastered forms
        }

        public EvolutionPhase currentPhase = EvolutionPhase.PureSlime;
        
        // Level thresholds for each phase
        public int[] levelThresholds = new int[]
        {
            1,   // Pure Slime (level 1-9)
            10,  // Human Form Acquired (level 10-19)
            20,  // Demon Lord Awakening (level 20-29)
            30   // True Dragon Ascension (level 30+)
        };

        // Stat multipliers per phase
        public float[] healthMultipliers = new float[] { 1.0f, 1.15f, 1.35f, 1.6f };
        public float[] damageMultipliers = new float[] { 1.0f, 1.1f, 1.25f, 1.5f };
        public float[] speedMultipliers = new float[] { 1.0f, 1.05f, 1.1f, 1.15f };

        // Track if form toggle is unlocked
        public bool isHumanFormUnlocked = false;

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
            
            // Start at Pure Slime phase
            currentPhase = EvolutionPhase.PureSlime;
            isHumanFormUnlocked = false;
        }

        public void FixedUpdate()
        {
            if (!characterBody && characterMaster)
            {
                characterBody = characterMaster.GetBody();
            }

            if (characterBody && Config.enableEvolutionSystem.Value)
            {
                // Check current level and update phase
                CheckLevelBasedPhase();
            }
        }

        /// <summary>
        /// Checks player level and updates evolution phase
        /// </summary>
        private void CheckLevelBasedPhase()
        {
            if (!characterBody)
            {
                return;
            }

            uint currentLevel = characterBody.level;
            EvolutionPhase newPhase = DeterminePhaseFromLevel((int)currentLevel);

            if (newPhase != currentPhase)
            {
                EvolveToPhase(newPhase);
            }
        }

        /// <summary>
        /// Determines evolution phase based on character level
        /// </summary>
        private EvolutionPhase DeterminePhaseFromLevel(int level)
        {
            if (level >= levelThresholds[3]) // Level 30+
            {
                return EvolutionPhase.TrueDragonAscension;
            }
            else if (level >= levelThresholds[2]) // Level 20-29
            {
                return EvolutionPhase.DemonLordAwakening;
            }
            else if (level >= levelThresholds[1]) // Level 10-19
            {
                return EvolutionPhase.HumanFormAcquired;
            }
            else // Level 1-9
            {
                return EvolutionPhase.PureSlime;
            }
        }

        /// <summary>
        /// Evolves to a new phase
        /// </summary>
        private void EvolveToPhase(EvolutionPhase newPhase)
        {
            if (newPhase == currentPhase)
            {
                return;
            }

            EvolutionPhase oldPhase = currentPhase;
            currentPhase = newPhase;

            // Update form unlock status
            if (currentPhase >= EvolutionPhase.HumanFormAcquired)
            {
                isHumanFormUnlocked = true;
            }

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
            string phaseDescription = GetPhaseDescription(newPhase);
            
            Chat.AddMessage($"<style=cIsUtility>═══════════════════════════</style>");
            Chat.AddMessage($"<style=cDeath>EVOLUTION COMPLETE!</style>");
            Chat.AddMessage($"<style=cIsUtility>{GetPhaseName(oldPhase)} → {phaseName}</style>");
            Chat.AddMessage($"<style=cStack>{phaseDescription}</style>");
            Chat.AddMessage($"<style=cIsUtility>═══════════════════════════</style>");
        }

        /// <summary>
        /// Gets display name for a phase
        /// </summary>
        private string GetPhaseName(EvolutionPhase phase)
        {
            switch (phase)
            {
                case EvolutionPhase.PureSlime:
                    return "Pure Slime";
                case EvolutionPhase.HumanFormAcquired:
                    return "Human Form Acquired";
                case EvolutionPhase.DemonLordAwakening:
                    return "Demon Lord Awakening";
                case EvolutionPhase.TrueDragonAscension:
                    return "True Dragon Ascension";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Gets description for a phase
        /// </summary>
        private string GetPhaseDescription(EvolutionPhase phase)
        {
            switch (phase)
            {
                case EvolutionPhase.PureSlime:
                    return "Focus: Survival, learning to devour enemies";
                case EvolutionPhase.HumanFormAcquired:
                    return "New abilities unlocked! Can now toggle between Slime and Human forms";
                case EvolutionPhase.DemonLordAwakening:
                    return "Enhanced forms and devastating power!";
                case EvolutionPhase.TrueDragonAscension:
                    return "Ultimate power achieved! Endgame god-mode unlocked";
                default:
                    return "";
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
                case EvolutionPhase.HumanFormAcquired:
                    characterBody.ApplyBuff(Buffs.evolutionPhase1Buff.buffIndex);
                    break;
                case EvolutionPhase.DemonLordAwakening:
                    characterBody.ApplyBuff(Buffs.evolutionPhase2Buff.buffIndex);
                    break;
                case EvolutionPhase.TrueDragonAscension:
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
        /// Checks if human form is unlocked (level 10+)
        /// </summary>
        public bool IsHumanFormUnlocked()
        {
            return isHumanFormUnlocked;
        }

        /// <summary>
        /// Checks if an ability is unlocked at current phase
        /// </summary>
        public bool IsAbilityUnlocked(string abilityName)
        {
            // Basic abilities available at Pure Slime phase
            if (currentPhase >= EvolutionPhase.PureSlime)
            {
                return true;
            }

            // Additional logic can be added for phase-specific abilities
            return false;
        }
    }
}
