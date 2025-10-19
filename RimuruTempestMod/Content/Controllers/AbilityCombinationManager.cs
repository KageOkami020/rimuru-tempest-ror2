using System;
using System.Collections.Generic;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;
using RimuruMod.Content.BuffControllers;

namespace RimuruMod.Content.Controllers
{
    /// <summary>
    /// Manages combining devoured abilities into new hybrid abilities
    /// Similar to Shigaraki mod's ability stealing system
    /// </summary>
    public class AbilityCombinationManager : MonoBehaviour
    {
        public CharacterMaster characterMaster;
        public CharacterBody characterBody;
        public AbilityStorageController abilityStorage;

        // Combination recipes - maps two enemy types to a combined buff controller
        private Dictionary<string, CombinedAbility> combinationRecipes;

        public void Awake()
        {
            characterMaster = gameObject.GetComponent<CharacterMaster>();
            InitializeCombinationRecipes();
        }

        public void Start()
        {
            if (characterMaster)
            {
                characterBody = characterMaster.GetBody();
                abilityStorage = characterMaster.GetComponent<AbilityStorageController>();
                
                if (!abilityStorage)
                {
                    abilityStorage = characterMaster.gameObject.AddComponent<AbilityStorageController>();
                }
            }
        }

        public void FixedUpdate()
        {
            if (!characterBody && characterMaster)
            {
                characterBody = characterMaster.GetBody();
            }
        }

        /// <summary>
        /// Initializes all possible ability combinations
        /// </summary>
        private void InitializeCombinationRecipes()
        {
            combinationRecipes = new Dictionary<string, CombinedAbility>();

            // Fire + Lightning = Plasma Storm (both offensive elements)
            AddCombination("LemurianBody", "WispBody", new CombinedAbility
            {
                name = "Plasma Storm",
                description = "Fire and Lightning merge into devastating plasma damage",
                buffController = typeof(PlasmaStormBuffController)
            });

            // Ice + Water = Absolute Zero (freezing combination)
            AddCombination("LesserWispBody", "JellyfishBody", new CombinedAbility
            {
                name = "Absolute Zero",
                description = "Freeze enemies solid with combined ice and water",
                buffController = typeof(AbsoluteZeroBuffController)
            });

            // Golem + Beetle Queen = Titan's Might (tanky combo)
            AddCombination("GolemBody", "BeetleQueenBody", new CombinedAbility
            {
                name = "Titan's Might",
                description = "Massive armor and regeneration boost",
                buffController = typeof(TitanMightBuffController)
            });

            // Imp + Wisp = Chaos Magic (magic combo)
            AddCombination("ImpBody", "GreaterWispBody", new CombinedAbility
            {
                name = "Chaos Magic",
                description = "Unpredictable magical attacks with bonus damage",
                buffController = typeof(ChaosMagicBuffController)
            });

            // Elder Lemurian + Golem = Dragon's Scales (defensive fire)
            AddCombination("LemurianBruiserBody", "GolemBody", new CombinedAbility
            {
                name = "Dragon's Scales",
                description = "Fiery armor that damages attackers",
                buffController = typeof(DragonScalesBuffController)
            });

            // Nullifier + Scavenger = Tactical Genius (intelligence combo)
            AddCombination("NullifierBody", "ScavBody", new CombinedAbility
            {
                name = "Tactical Genius",
                description = "Enhanced cooldown reduction and item luck",
                buffController = typeof(TacticalGeniusBuffController)
            });

            // Parent + Mushroom = Nature's Blessing (healing combo)
            AddCombination("ParentBody", "MiniMushroomBody", new CombinedAbility
            {
                name = "Nature's Blessing",
                description = "Powerful healing and regeneration aura",
                buffController = typeof(NatureBlessingBuffController)
            });

            // Vagrant + Vermin = Swarm Intelligence (AoE combo)
            AddCombination("VagrantBody", "VerminBody", new CombinedAbility
            {
                name = "Swarm Intelligence",
                description = "Summon orbital attacks that seek enemies",
                buffController = typeof(SwarmIntelligenceBuffController)
            });

            // Beetle Guard + Bison = Unstoppable Force (charge combo)
            AddCombination("BeetleGuardBody", "BisonBody", new CombinedAbility
            {
                name = "Unstoppable Force",
                description = "Charge attacks deal massive knockback",
                buffController = typeof(UnstoppableForceBuffController)
            });

            // Clay Dunestrider + Magma Worm = Volcanic Fury (ground control)
            AddCombination("ClayBossBody", "MagmaWormBody", new CombinedAbility
            {
                name = "Volcanic Fury",
                description = "Create areas of volcanic devastation",
                buffController = typeof(VolcanicFuryBuffController)
            });
        }

        /// <summary>
        /// Helper to add a combination recipe
        /// </summary>
        private void AddCombination(string enemy1, string enemy2, CombinedAbility ability)
        {
            // Sort alphabetically for consistent keys
            var sorted = new[] { enemy1, enemy2 };
            Array.Sort(sorted);
            string key = $"{sorted[0]}_{sorted[1]}";
            
            combinationRecipes[key] = ability;
        }

        /// <summary>
        /// Attempts to combine two abilities
        /// </summary>
        public bool TryCombineAbilities(string ability1, string ability2)
        {
            if (!abilityStorage || !abilityStorage.CanCombineAbilities(ability1, ability2))
            {
                return false;
            }

            // Get combination key
            var sorted = new[] { ability1, ability2 };
            Array.Sort(sorted);
            string key = $"{sorted[0]}_{sorted[1]}";

            // Check if combination exists
            if (!combinationRecipes.ContainsKey(key))
            {
                Chat.AddMessage("<style=cIsHealth>These abilities cannot be combined.</style>");
                return false;
            }

            // Get the combined ability
            CombinedAbility combinedAbility = combinationRecipes[key];

            // Consume the abilities from storage
            if (!abilityStorage.ConsumeAbilitiesForCombination(ability1, ability2))
            {
                return false;
            }

            // Apply the combined ability buff
            ApplyCombinedAbility(combinedAbility);

            return true;
        }

        /// <summary>
        /// Applies a combined ability to the character
        /// </summary>
        private void ApplyCombinedAbility(CombinedAbility ability)
        {
            if (!characterMaster)
            {
                return;
            }

            // Show notification
            Chat.AddMessage($"<style=cDeath>═══════════════════════════</style>");
            Chat.AddMessage($"<style=cIsUtility>ABILITY FUSION!</style>");
            Chat.AddMessage($"<style=cIsUtility>{ability.name}</style>");
            Chat.AddMessage($"<style=cStack>{ability.description}</style>");
            Chat.AddMessage($"<style=cDeath>═══════════════════════════</style>");

            // Add the buff controller component
            if (ability.buffController != null)
            {
                var existing = characterMaster.GetComponent(ability.buffController);
                if (!existing)
                {
                    characterMaster.gameObject.AddComponent(ability.buffController);
                }
                else
                {
                    // Refresh timer if it's a RimuruBaseBuffController
                    if (existing is RimuruBaseBuffController baseController)
                    {
                        baseController.RefreshTimers();
                    }
                }
            }

            // Play effect
            if (characterBody)
            {
                AkSoundEngine.PostEvent("RimuruAnalyse", characterBody.gameObject);
                EffectManager.SpawnEffect(AssetsRimuru.devourskillgetEffect, new EffectData
                {
                    origin = characterBody.corePosition,
                    scale = 2f,
                    rotation = Quaternion.identity
                }, true);
            }
        }

        /// <summary>
        /// Gets info about a combination if it exists
        /// </summary>
        public CombinedAbility GetCombinationInfo(string ability1, string ability2)
        {
            var sorted = new[] { ability1, ability2 };
            Array.Sort(sorted);
            string key = $"{sorted[0]}_{sorted[1]}";

            if (combinationRecipes.ContainsKey(key))
            {
                return combinationRecipes[key];
            }

            return null;
        }

        /// <summary>
        /// Gets all known combination recipes
        /// </summary>
        public Dictionary<string, CombinedAbility> GetAllRecipes()
        {
            return new Dictionary<string, CombinedAbility>(combinationRecipes);
        }

        public class CombinedAbility
        {
            public string name;
            public string description;
            public Type buffController;
        }
    }
}
