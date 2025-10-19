using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.Controllers
{
    /// <summary>
    /// Manages storage and tracking of devoured abilities for combination system
    /// </summary>
    public class AbilityStorageController : MonoBehaviour
    {
        public CharacterMaster characterMaster;
        public CharacterBody characterBody;
        
        // Stores the names of devoured enemy types
        public List<string> storedAbilities = new List<string>();
        
        // Maximum number of abilities that can be stored
        public int maxStoredAbilities = 10;
        
        // Tracks which abilities have been combined
        public HashSet<string> usedAbilityCombinations = new HashSet<string>();

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
        }

        public void FixedUpdate()
        {
            if (!characterBody && characterMaster)
            {
                characterBody = characterMaster.GetBody();
            }
        }

        /// <summary>
        /// Adds a devoured ability to storage
        /// </summary>
        public bool AddAbility(string enemyName)
        {
            // Don't store duplicates
            if (storedAbilities.Contains(enemyName))
            {
                return false;
            }

            // Check if at capacity
            if (storedAbilities.Count >= maxStoredAbilities)
            {
                // Remove oldest ability
                storedAbilities.RemoveAt(0);
            }

            storedAbilities.Add(enemyName);
            
            // Notify player
            if (characterBody)
            {
                Chat.AddMessage($"<style=cIsUtility>Stored ability from {GetEnemyDisplayName(enemyName)}</style>");
            }

            return true;
        }

        /// <summary>
        /// Checks if a specific ability combination is available
        /// </summary>
        public bool CanCombineAbilities(string ability1, string ability2)
        {
            if (!storedAbilities.Contains(ability1) || !storedAbilities.Contains(ability2))
            {
                return false;
            }

            string combinationKey = GetCombinationKey(ability1, ability2);
            return !usedAbilityCombinations.Contains(combinationKey);
        }

        /// <summary>
        /// Marks a combination as used and removes the abilities from storage
        /// </summary>
        public bool ConsumeAbilitiesForCombination(string ability1, string ability2)
        {
            if (!CanCombineAbilities(ability1, ability2))
            {
                return false;
            }

            storedAbilities.Remove(ability1);
            storedAbilities.Remove(ability2);
            
            string combinationKey = GetCombinationKey(ability1, ability2);
            usedAbilityCombinations.Add(combinationKey);

            return true;
        }

        /// <summary>
        /// Gets a unique key for an ability combination
        /// </summary>
        private string GetCombinationKey(string ability1, string ability2)
        {
            // Sort alphabetically to ensure consistent keys
            var sorted = new[] { ability1, ability2 }.OrderBy(x => x).ToArray();
            return $"{sorted[0]}_{sorted[1]}";
        }

        /// <summary>
        /// Gets a user-friendly display name for an enemy
        /// </summary>
        public static string GetEnemyDisplayName(string enemyName)
        {
            // Strip "Body" suffix and add spaces before capitals
            string cleaned = enemyName.Replace("Body", "");
            return System.Text.RegularExpressions.Regex.Replace(cleaned, "([a-z])([A-Z])", "$1 $2");
        }

        /// <summary>
        /// Gets all available ability combinations
        /// </summary>
        public List<AbilityCombination> GetAvailableCombinations()
        {
            List<AbilityCombination> combinations = new List<AbilityCombination>();

            for (int i = 0; i < storedAbilities.Count; i++)
            {
                for (int j = i + 1; j < storedAbilities.Count; j++)
                {
                    string ability1 = storedAbilities[i];
                    string ability2 = storedAbilities[j];

                    if (CanCombineAbilities(ability1, ability2))
                    {
                        combinations.Add(new AbilityCombination
                        {
                            ability1 = ability1,
                            ability2 = ability2,
                            combinationKey = GetCombinationKey(ability1, ability2)
                        });
                    }
                }
            }

            return combinations;
        }

        public class AbilityCombination
        {
            public string ability1;
            public string ability2;
            public string combinationKey;
        }
    }
}
