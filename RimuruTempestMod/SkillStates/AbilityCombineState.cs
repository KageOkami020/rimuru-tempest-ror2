using EntityStates;
using RoR2;
using RoR2.UI;
using UnityEngine;
using RimuruMod.Modules;
using RimuruMod.Content.Controllers;
using System.Collections.Generic;
using System.Linq;

namespace RimuruMod.SkillStates
{
    /// <summary>
    /// Special skill state for manually combining stored abilities
    /// Shows available combinations and allows player to select one
    /// </summary>
    public class AbilityCombineState : BaseSkillState
    {
        private AbilityStorageController abilityStorage;
        private AbilityCombinationManager combinationManager;
        private List<AbilityStorageController.AbilityCombination> availableCombinations;
        private int selectedIndex = 0;
        private float baseDuration = 5f;
        private bool hasCombined = false;

        // Animation constants
        private const string ANIMATION_LAYER = "Gesture, Override";
        private const string ANIMATION_NAME = "Analyze";
        private const string ANIMATION_PLAYBACK_RATE = "Analyze.playbackRate";
        private const string SOUND_EVENT_ANALYZE = "RimuruAnalyse";

        public override void OnEnter()
        {
            base.OnEnter();

            // Get components
            if (characterBody && characterBody.master)
            {
                abilityStorage = characterBody.master.GetComponent<AbilityStorageController>();
                combinationManager = characterBody.master.GetComponent<AbilityCombinationManager>();
            }

            // Check if combination system is enabled
            if (!Config.enableAbilityCombination.Value || !abilityStorage || !combinationManager)
            {
                Chat.AddMessage("<style=cIsHealth>Ability combination system is not available.</style>");
                outer.SetNextStateOnNextUpdate(new EntityStates.Idle());
                return;
            }

            // Get available combinations
            availableCombinations = abilityStorage.GetAvailableCombinations();

            if (availableCombinations.Count == 0)
            {
                Chat.AddMessage("<style=cIsHealth>No ability combinations available.</style>");
                Chat.AddMessage($"<style=cStack>You have {abilityStorage.storedAbilities.Count} stored abilities. Need at least 2 compatible abilities to combine.</style>");
                outer.SetNextStateOnNextUpdate(new EntityStates.Idle());
                return;
            }

            // Show available combinations
            ShowAvailableCombinations();

            // Play animation
            PlayAnimation(ANIMATION_LAYER, ANIMATION_NAME, ANIMATION_PLAYBACK_RATE, baseDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!isAuthority)
            {
                return;
            }

            // Check for input to cycle through combinations
            if (inputBank)
            {
                // Secondary skill button to cycle forward
                if (inputBank.skill2.justPressed && availableCombinations.Count > 0)
                {
                    selectedIndex = (selectedIndex + 1) % availableCombinations.Count;
                    ShowSelectedCombination();
                }

                // Utility skill button to cycle backward
                if (inputBank.skill3.justPressed && availableCombinations.Count > 0)
                {
                    selectedIndex--;
                    if (selectedIndex < 0)
                    {
                        selectedIndex = availableCombinations.Count - 1;
                    }
                    ShowSelectedCombination();
                }

                // Primary skill button to confirm combination
                if (inputBank.skill1.justPressed && availableCombinations.Count > 0)
                {
                    TryCombineSelected();
                    return;
                }
            }

            // Auto-exit after duration
            if (fixedAge >= baseDuration && !hasCombined)
            {
                outer.SetNextStateOnNextUpdate(new EntityStates.Idle());
            }
        }

        private void ShowAvailableCombinations()
        {
            Chat.AddMessage("<style=cIsUtility>═══ ABILITY COMBINATION ═══</style>");
            Chat.AddMessage($"<style=cStack>Available combinations: {availableCombinations.Count}</style>");
            Chat.AddMessage("<style=cStack>Use Secondary/Utility to navigate, Primary to combine</style>");
            ShowSelectedCombination();
        }

        private void ShowSelectedCombination()
        {
            if (availableCombinations.Count == 0)
            {
                return;
            }

            var combination = availableCombinations[selectedIndex];
            var info = combinationManager.GetCombinationInfo(combination.ability1, combination.ability2);

            if (info != null)
            {
                Chat.AddMessage($"<style=cIsUtility>[{selectedIndex + 1}/{availableCombinations.Count}] {info.name}</style>");
                Chat.AddMessage($"<style=cStack>{AbilityStorageController.GetEnemyDisplayName(combination.ability1)} + {AbilityStorageController.GetEnemyDisplayName(combination.ability2)}</style>");
                Chat.AddMessage($"<style=cDeath>{info.description}</style>");
            }
            else
            {
                Chat.AddMessage($"<style=cStack>[{selectedIndex + 1}/{availableCombinations.Count}] {AbilityStorageController.GetEnemyDisplayName(combination.ability1)} + {AbilityStorageController.GetEnemyDisplayName(combination.ability2)}</style>");
            }
        }

        private void TryCombineSelected()
        {
            if (availableCombinations.Count == 0 || selectedIndex >= availableCombinations.Count)
            {
                return;
            }

            var combination = availableCombinations[selectedIndex];

            if (combinationManager.TryCombineAbilities(combination.ability1, combination.ability2))
            {
                hasCombined = true;
                
                // Play success effect
                if (characterBody)
                {
                    AkSoundEngine.PostEvent(SOUND_EVENT_ANALYZE, characterBody.gameObject);
                    EffectManager.SpawnEffect(AssetsRimuru.devourskillgetEffect, new EffectData
                    {
                        origin = characterBody.corePosition,
                        scale = 2f,
                        rotation = Quaternion.identity
                    }, true);
                }

                // Exit state
                outer.SetNextStateOnNextUpdate(new EntityStates.Idle());
            }
            else
            {
                Chat.AddMessage("<style=cIsHealth>Failed to combine abilities.</style>");
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
