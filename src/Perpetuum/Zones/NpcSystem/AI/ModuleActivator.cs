using Perpetuum.EntityFramework;
using Perpetuum.Modules;
using Perpetuum.Modules.EffectModules;
using Perpetuum.Modules.Weapons;
using Perpetuum.Timers;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.Terrains;
using System;

namespace Perpetuum.Zones.NpcSystem.AI
{
    public class ModuleActivator :
        IEntityVisitor<WeaponModule>,
        IEntityVisitor<MissileWeaponModule>,
        IEntityVisitor<ArmorRepairModule>,
        IEntityVisitor<ShieldGeneratorModule>,
        IEntityVisitor<SensorJammerModule>,
        IEntityVisitor<SensorDampenerModule>,
        IEntityVisitor<WebberModule>,
        IEntityVisitor<EnergyNeutralizerModule>,
        IEntityVisitor<EnergyVampireModule>,
        IEntityVisitor<SensorBoosterModule>,
        IEntityVisitor<ArmorHardenerModule>,
        IEntityVisitor<BlobEmissionModulatorModule>,
        IEntityVisitor<TargetBlinderModule>,
        IEntityVisitor<CoreBoosterModule>,
        IEntityVisitor<TargetPainterModule>,
        IEntityVisitor<RemoteControlledDrillerModule>,
        IEntityVisitor<RemoteControlledHarvesterModule>,
        IEntityVisitor<RemoteControllerModule>,
        IEntityVisitor<ScorcherModule>,
        IEntityVisitor<NoxModule>,
        IEntityVisitor<EnergyTransfererModule>,
        IEntityVisitor<RemoteArmorRepairModule>
    {
        public ModuleActivator(ActiveModule module)
        {
            this.module = module;
            timer = new IntervalTimer(TimeSpan.FromSeconds(1), true);
        }

        private const double ENERGY_INJECTOR_THRESHOLD = 0.65;
        private const double ARMOR_REPAIR_THRESHOLD = 0.95;
        private const double ARMOR_REPAIR_CORE_THRESHOLD = 0.35;
        private const double SENSOR_DAMPENER_CORE_THRESHOLD = 0.55;
        private const double SHIELD_ARMOR_THRESHOLD = 0.35;
        private const double SENSOR_JAMMER_CORE_THRESHOLD = 0.55;
        private const double WEBBER_CORE_THRESHOLD = 0.55;
        private const double PAINTER_CORE_THRESHOLD = 0.65;
        private const double BLOBBER_CORE_THRESHOLD = 0.55;
        private const double BLINDER_CORE_THRESHOLD = 0.55;
        private const double ENERGY_NEUTRALIZER_CORE_THRESHOLD = 0.55;
        private const double ENERGY_VAMPIRE_CORE_THRESHOLD = 0.05;

        private readonly IntervalTimer timer;
        private readonly ActiveModule module;

        public void Update(TimeSpan time)
        {
            _ = timer.Update(time);

            if (!timer.Passed)
            {
                return;
            }

            timer.Reset();

            if (module.State.Type != ModuleStateType.Idle)
            {
                return;
            }

            module.AcceptVisitor(this);
        }

        public void Visit(MissileWeaponModule module)
        {
            bool hasShieldEffect = module.ParentRobot.HasShieldEffect;

            if (hasShieldEffect)
            {
                return;
            }

            UnitLock primaryLock = module.ParentRobot.GetFinishedPrimaryLock();

            if (primaryLock == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(primaryLock.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult result = visibility.GetLineOfSight(true);

            TryActiveModule(result, primaryLock);
        }

        public void Visit(WeaponModule module)
        {
            bool hasShieldEffect = module.ParentRobot.HasShieldEffect;

            if (hasShieldEffect)
            {
                return;
            }

            UnitLock primaryLock = module.ParentRobot.GetFinishedPrimaryLock();

            if (primaryLock == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(primaryLock.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult result = visibility.GetLineOfSight(false);

            TryActiveModule(result, primaryLock);
        }

        public void Visit(ArmorRepairModule module)
        {
            if (module.ParentRobot.ArmorPercentage >= ARMOR_REPAIR_THRESHOLD)
            {
                return;
            }

            if (module.ParentRobot.CorePercentage < ARMOR_REPAIR_CORE_THRESHOLD)
            {
                return;
            }

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(ShieldGeneratorModule module)
        {
            if (module.ParentRobot.ArmorPercentage >= SHIELD_ARMOR_THRESHOLD)
            {
                return;
            }

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(SensorJammerModule module)
        {
            if (module.ParentRobot.CorePercentage < SENSOR_JAMMER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(SensorDampenerModule module)
        {
            if (module.ParentRobot.CorePercentage < SENSOR_DAMPENER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(WebberModule module)
        {
            if (module.ParentRobot.CorePercentage < WEBBER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(TargetPainterModule module)
        {
            if (module.ParentRobot.CorePercentage < PAINTER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(BlobEmissionModulatorModule module)
        {
            if (module.ParentRobot.Zone.Configuration.Protected)
            {
                return;
            }

            if (module.ParentRobot.CorePercentage < BLOBBER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(TargetBlinderModule module)
        {
            if (module.ParentRobot.CorePercentage < BLINDER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(EnergyNeutralizerModule module)
        {
            if (module.ParentRobot.CorePercentage < ENERGY_NEUTRALIZER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult r = visibility.GetLineOfSight(false);

            if (r != null && r.hit)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(EnergyVampireModule module)
        {
            if (module.ParentRobot.CorePercentage < ENERGY_VAMPIRE_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult r = visibility.GetLineOfSight(false);

            if (r != null && r.hit)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(SensorBoosterModule module)
        {
            if (module.State.Type == ModuleStateType.Idle)
            {
                module.State.SwitchTo(ModuleStateType.AutoRepeat);
            }
        }

        public void Visit(ArmorHardenerModule module)
        {
            if (module.State.Type == ModuleStateType.Idle)
            {
                module.State.SwitchTo(ModuleStateType.AutoRepeat);
            }
        }

        public void Visit(CoreBoosterModule module)
        {
            if (module.ParentRobot.CorePercentage > ENERGY_INJECTOR_THRESHOLD)
            {
                return;
            }

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(RemoteControlledDrillerModule module)
        {
            TerrainLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockIndustrialTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(RemoteControlledHarvesterModule module)
        {
            TerrainLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockIndustrialTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(RemoteControllerModule module)
        {
            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(ScorcherModule module)
        {
            if (module.ParentRobot.CorePercentage < ENERGY_NEUTRALIZER_CORE_THRESHOLD)
            {
                return;
            }

            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult r = visibility.GetLineOfSight(false);

            if (r != null && r.hit)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(NoxModule module)
        {
            if (module.State.Type == ModuleStateType.Idle)
            {
                module.State.SwitchTo(ModuleStateType.AutoRepeat);
            }
        }

        public void Visit(EnergyTransfererModule module)
        {
            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null || lockTarget.Target.HasShieldEffect)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult r = visibility.GetLineOfSight(false);

            if (r != null && r.hit)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        public void Visit(RemoteArmorRepairModule module)
        {
            UnitLock lockTarget = ((Creature)module.ParentRobot).SelectOptimalLockTargetFor(module);

            if (lockTarget == null || lockTarget.Target.HasShieldEffect)
            {
                return;
            }

            Units.IUnitVisibility visibility = module.ParentRobot.GetVisibility(lockTarget.Target);

            if (visibility == null)
            {
                return;
            }

            LOSResult r = visibility.GetLineOfSight(false);

            if (r != null && r.hit)
            {
                return;
            }

            module.Lock = lockTarget;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }

        private void TryActiveModule(LOSResult result, UnitLock primaryLock)
        {
            if (result.hit && !result.blockingFlags.HasFlag(BlockingFlags.Plant))
            {
                return;
            }

            module.Lock = primaryLock;
            module.State.SwitchTo(ModuleStateType.Oneshot);
        }
    }
}
