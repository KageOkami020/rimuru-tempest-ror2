using System;
using UnityEngine;
using RoR2;
using RimuruMod.Modules;

namespace RimuruMod.Content.BuffControllers
{
    /// <summary>
    /// Absolute Zero - Combination of Lesser Wisp (Ice) + Jellyfish (Water)
    /// Freezes enemies with extreme cold
    /// </summary>
    public class AbsoluteZeroBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.absoluteZeroBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Absolute Zero Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Chaos Magic - Combination of Imp + Greater Wisp
    /// Unpredictable magical attacks
    /// </summary>
    public class ChaosMagicBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.chaosMagicBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Chaos Magic Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Dragon's Scales - Combination of Elder Lemurian + Golem
    /// Fiery armor that damages attackers
    /// </summary>
    public class DragonScalesBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.dragonScalesBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Dragon's Scales Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Nature's Blessing - Combination of Parent + Mushroom
    /// Powerful healing aura
    /// </summary>
    public class NatureBlessingBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.natureBlessingBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Nature's Blessing Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Swarm Intelligence - Combination of Vagrant + Vermin
    /// Orbital attacks that seek enemies
    /// </summary>
    public class SwarmIntelligenceBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.swarmIntelligenceBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Swarm Intelligence Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Unstoppable Force - Combination of Beetle Guard + Bison
    /// Powerful charge attacks
    /// </summary>
    public class UnstoppableForceBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.unstoppableForceBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Unstoppable Force Skill</style> acquisition successful.");
        }
    }

    /// <summary>
    /// Volcanic Fury - Combination of Clay Dunestrider + Magma Worm
    /// Create volcanic devastation zones
    /// </summary>
    public class VolcanicFuryBuffController : RimuruBaseBuffController
    {
        public override void Awake()
        {
            base.Awake();
            isPermaBuff = false;
            buffdef = Buffs.volcanicFuryBuff;
            lifetime = Config.devourBuffLength.Value;
        }

        public void Start()
        {
            Chat.AddMessage("<style=cIsUtility>Volcanic Fury Skill</style> acquisition successful.");
        }
    }
}
