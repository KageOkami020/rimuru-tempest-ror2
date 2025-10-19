using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.BuffControllers
{
    /// <summary>
    /// Tactical Genius - Combination of Nullifier + Scavenger
    /// Enhanced cooldown reduction and better item luck
    /// </summary>
    public class TacticalGeniusBuffController : RimuruBaseBuffController
    {
        private const int HITS_FOR_COOLDOWN = 3; // Reduced from Nullifier's 4
        private const float COOLDOWN_REDUCTION = 2f; // Increased from 1s
        private const float LUCK_BONUS = 5f;

        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.tacticalGeniusBuff;
            lifetime = Config.devourBuffLength.Value;
            Hook();
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Tactical Genius Skill</style> acquisition successful.");
        }

        private void Hook()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        private void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);

            if (damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if (attackerBody && attackerBody.HasBuff(Buffs.tacticalGeniusBuff))
                {
                    int buffcount = attackerBody.GetBuffCount(Buffs.tacticalGeniusBuffStacks);

                    if (buffcount >= HITS_FOR_COOLDOWN)
                    {
                        attackerBody.skillLocator.DeductCooldownFromAllSkillsServer(COOLDOWN_REDUCTION);
                        attackerBody.ApplyBuff(Buffs.tacticalGeniusBuffStacks.buffIndex, 0);
                    }
                    else
                    {
                        attackerBody.ApplyBuff(Buffs.tacticalGeniusBuffStacks.buffIndex, buffcount + 1);
                    }
                }
            }
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            
            if (self && self.HasBuff(Buffs.tacticalGeniusBuff) && self.master)
            {
                self.master.luck += LUCK_BONUS;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            On.RoR2.GlobalEventManager.OnHitEnemy -= GlobalEventManager_OnHitEnemy;
            On.RoR2.CharacterBody.RecalculateStats -= CharacterBody_RecalculateStats;
        }
    }
}
