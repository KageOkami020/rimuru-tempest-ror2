using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.BuffControllers
{
    /// <summary>
    /// Titan's Might - Combination of Golem + Beetle Queen
    /// Massive armor and health regeneration
    /// </summary>
    public class TitanMightBuffController : RimuruBaseBuffController
    {
        private const float ARMOR_BONUS = 150f;
        private const float REGEN_BONUS = 0.05f; // 5% of max health per second

        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.titanMightBuff;
            lifetime = Config.devourBuffLength.Value;
            Hook();
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Titan's Might Skill</style> acquisition successful.");
        }

        private void Hook()
        {
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            
            if (self && self.HasBuff(Buffs.titanMightBuff))
            {
                self.armor += ARMOR_BONUS;
                self.regen += self.maxHealth * REGEN_BONUS;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        }
    }
}
