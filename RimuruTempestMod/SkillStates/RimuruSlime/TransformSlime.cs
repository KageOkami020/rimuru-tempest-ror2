using EntityStates;
using R2API.Networking.Interfaces;
using RimuruMod.Content.BuffControllers;
using RimuruMod.Modules.Networking;
using RimuruMod.Modules.Survivors;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RimuruMod.SkillStates
{
    public class TransformSlime : BaseSkillState
    {
        public float oldHealth;
        public RimuruMasterController masterController;

        public override void OnEnter()
        {
            base.OnEnter();
            
            // Check if human form is unlocked (level 10+)
            if (characterBody && characterBody.master)
            {
                var evolutionController = characterBody.master.GetComponent<Modules.Survivors.EvolutionController>();
                if (evolutionController && !evolutionController.IsHumanFormUnlocked())
                {
                    // Human form not unlocked yet, cancel transformation
                    Chat.AddMessage("<style=cIsHealth>Human form unlocks at level 10!</style>");
                    this.outer.SetNextStateToMain();
                    return;
                }
            }
            
            masterController = characterBody.master.GetComponent<RimuruMasterController>();
            oldHealth = characterBody.healthComponent.health;
            //new TransformBody(characterBody.master.netId, oldHealth, (int)TransformBody.TargetBody.HUMAN).Send(R2API.Networking.NetworkDestination.Clients);

            if (NetworkServer.active)
            {
                Modules.StaticValues.TransformBodyType(Modules.StaticValues.TargetBody.HUMAN, characterBody.master);
            }
            AkSoundEngine.PostEvent("RimuruTransform", base.gameObject);
        }


        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge > 0f && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            new UpdateControllers(characterBody.master.netId, oldHealth).Send(R2API.Networking.NetworkDestination.Clients);
            masterController.setHealthToValue = true;
            masterController.oldHealth = oldHealth;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}