using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.BuffControllers
{
    /// <summary>
    /// Plasma Storm - Combination of Lemurian (Fire) + Wisp (Lightning)
    /// Creates devastating plasma damage that burns and shocks
    /// </summary>
    public class PlasmaStormBuffController : RimuruBaseBuffController
    {
        private float damageTimer = 0f;
        private const float DAMAGE_INTERVAL = 1.5f;
        private const float PLASMA_RADIUS = 15f;
        private const float PLASMA_DAMAGE_COEFFICIENT = 1.5f;

        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.plasmaStormBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cDeath>Plasma Storm Skill</style> acquisition successful.");
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            damageTimer += Time.fixedDeltaTime;
            if (damageTimer >= DAMAGE_INTERVAL && body)
            {
                damageTimer = 0f;
                TriggerPlasmaStorm();
            }
        }

        private void TriggerPlasmaStorm()
        {
            // Spawn visual effect
            EffectManager.SpawnEffect(AssetsRimuru.elderlemurianexplosionEffect, new EffectData
            {
                origin = body.corePosition,
                scale = PLASMA_RADIUS / 10f
            }, true);

            // Create blast that both burns and shocks
            new BlastAttack
            {
                attacker = body.gameObject,
                teamIndex = body.teamComponent.teamIndex,
                falloffModel = BlastAttack.FalloffModel.None,
                baseDamage = body.damage * PLASMA_DAMAGE_COEFFICIENT,
                damageType = new RoR2.DamageTypeCombo(DamageType.IgniteOnHit | DamageType.Shock5s, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                damageColorIndex = DamageColorIndex.DeathMark,
                baseForce = 500f,
                position = body.corePosition,
                radius = PLASMA_RADIUS,
                procCoefficient = 1f,
                attackerFiltering = AttackerFiltering.NeverHitSelf
            }.Fire();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
