using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Players;
using Perpetuum.StateMachines;
using Perpetuum.Timers;
using Perpetuum.Units;
using Perpetuum.Zones.DamageProcessors;
using Perpetuum.Zones.Effects;
using Perpetuum.Zones.Locking;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem.AI;
using Perpetuum.Zones.NpcSystem.AI.Behaviors;
using Perpetuum.Zones.NpcSystem.AI.CombatDrones;
using Perpetuum.Zones.NpcSystem.AI.IndustrialDrones;
using Perpetuum.Zones.NpcSystem.Flocks;
using Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using Perpetuum.Zones.Scanning.Results;
using Perpetuum.Zones.Terrains;
using Perpetuum.Zones.Terrains.Materials;
using Perpetuum.Zones.Terrains.Materials.Minerals;
using Perpetuum.Zones.Terrains.Materials.Plants;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Zones.NpcSystem
{
    public class SmartCreature : Creature
    {
        private const double AggroRange = 30;
        private const double BestComnatRangeModifier = 0.9;
        private const double BaseCallForHelpArmorThreshold = 0.2;
        private readonly TimeKeeper debounceBodyPull = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly TimeKeeper debounceLockChange = new TimeKeeper(TimeSpan.FromSeconds(2.5));
        private readonly IntervalTimer pseudoUpdateFreq = new IntervalTimer(TimeSpan.FromMilliseconds(650));
        private Lazy<int> maxActionRange;
        private Lazy<int> optimalActionRange;
        private TimeSpan lastHelpCalled;

        [CanBeNull]
        private ISmartCreatureGroup group;

        public StackFSM AI { get; private set; }

        public Behavior Behavior { get; set; }

        public double HomeRange { get; set; }

        public Position HomePosition { get; set; }

        public ThreatManager ThreatManager { get; }

        public PseudoThreatManager PseudoThreatManager { get; }

        public IndustrialValueManager IndustrialValueManager { get; }

        public int BestActionRange => optimalActionRange.Value;

        public int MaxActionRange => maxActionRange.Value;

        public bool IsInHomeRange => CurrentPosition.IsInRangeOf2D(HomePosition, HomeRange);

        public ISmartCreatureGroup Group => group;

        public virtual bool IsStationary => MaxSpeed.IsZero();

        public virtual double CallForHelpArmorThreshold => BaseCallForHelpArmorThreshold;

        public bool CallForHelp { private get; set; }

        public NpcBossInfo BossInfo { get; set; }

        public SmartCreature()
        {
            RecalculateMaxCombatRange();
            RecalculateOptimalCombatRange();
            ThreatManager = new ThreatManager();
            AI = new StackFSM();
            PseudoThreatManager = new PseudoThreatManager();
            IndustrialValueManager = new IndustrialValueManager();
        }

        public virtual void LookingForHostiles()
        {
            foreach (IUnitVisibility visibility in GetVisibleUnits())
            {
                AddBodyPullThreat(visibility.Target);
            }
        }

        public void LookingForMiningTargets()
        {
            IndustrialValueManager.Clear();
            Area area = Zone.CreateArea(CurrentPosition, BestActionRange);
            MaterialType[] availableMaterialTypes =
                Zone.Terrain
                    .GetAvailableMineralTypes()
                    .Where(x => x != MaterialType.EnergyMineral)
                    .ToArray();
            foreach (MaterialType materialType in availableMaterialTypes)
            {
                MineralScanResultBuilder builder = MineralScanResultBuilder.Create(Zone, materialType);
                builder.ScanArea = area;
                builder.ScanAccuracy = 100.0;
                MineralScanResult result = builder.Build();
                if (Zone.Terrain.GetMaterialLayer(materialType) is MineralLayer mineralLayer)
                {
                    if (result.FoundAny)
                    {
                        IEnumerable<Position> valuablePositions = result.Area
                            .GetPositions()
                            .Where(x => mineralLayer.HasMineral(x))
                            .Select(x => Zone.FixZ(x))
                            .Where(x => x.IsInRangeOf3D(PositionWithHeight, BestActionRange * 0.9));
                        foreach (Position valuablePosition in valuablePositions)
                        {
                            MineralNode mineralNode = mineralLayer.GetNode(valuablePosition);
                            IndustrialValueManager
                                .AddOrUpdateIndustrialTargetWithValue(
                                    valuablePosition.Center,
                                    mineralNode.GetValue(valuablePosition));
                        }
                    }
                }
            }
        }

        public void ProcessIndustrialTarget(
            Position position,
            double value)
        {
            if (value <= 0 || !Zone.IsInLineOfSight(this, position, false).hit)
            {
                IndustrialValueManager.Remove(position);
                GetLockByPosition(position)?.Cancel();
            }

            IndustrialValueManager.AddOrUpdateIndustrialTargetWithValue(position, value);
        }

        public void LookingForHarvestingTargets()
        {
            IndustrialValueManager.Clear();
            Area area = Zone.CreateArea(CurrentPosition, BestActionRange);
            PlantType[] availablePlantTypes = Zone.Terrain.GetAvailablePlantTypes();
            foreach (PlantType plantType in availablePlantTypes)
            {
                List<Position> plants = Zone.GetPlantPositionsInArea(plantType, area);
                if (plants.Any())
                {
                    IEnumerable<Position> valuablePositions = plants
                        .Select(x => Zone.FixZ(x))
                        .Where(x => x.IsInRangeOf2D(CurrentPosition, BestActionRange * 0.9));
                    foreach (Position valuablePosition in valuablePositions)
                    {
                        PlantInfo plant = Zone.Terrain.Plants.GetValue(valuablePosition.intX, valuablePosition.intY);
                        if (plant.material > 0)
                        {
                            IndustrialValueManager
                                .AddOrUpdateIndustrialTargetWithValue(
                                    valuablePosition.Center,
                                    plant.material);
                        }
                    }
                }
            }
        }

        public void AddPseudoThreat(Unit hostile)
        {
            PseudoThreatManager.AddOrRefreshExisting(hostile);
        }

        public void SetGroup(ISmartCreatureGroup group)
        {
            this.group = group;
        }

        public void RecalculateOptimalCombatRange()
        {
            optimalActionRange = new Lazy<int>(CalculateCombatRange);
        }

        public void RecalculateMaxCombatRange()
        {
            maxActionRange = new Lazy<int>(CalculateMaxCombatRange);
        }

        protected override void OnPropertyChanged(ItemProperty property)
        {
            base.OnPropertyChanged(property);
            switch (property.Field)
            {
                case AggregateField.locking_range:
                    {
                        RecalculateOptimalCombatRange();
                        RecalculateMaxCombatRange();

                        break;
                    }
                case AggregateField.armor_current:
                    {
                        double percentage = Armor.Ratio(ArmorMax);

                        if (percentage <= CallForHelpArmorThreshold)
                        {
                            CallingForHelp();
                        }

                        break;
                    }
            }
        }

        protected override void OnUnitLockStateChanged(Lock @lock)
        {
            if (!debounceLockChange.Expired)
            {
                return;
            }

            if (!(@lock is UnitLock unitLock))
            {
                return;
            }

            if (unitLock.Target != this)
            {
                return;
            }

            if (unitLock.State != LockState.Locked)
            {
                return;
            }

            double threatValue = unitLock.Primary
                ? Threat.LOCK_PRIMARY
                : Threat.LOCK_SECONDARY;
            AddThreat(unitLock.Owner, new Threat(ThreatType.Lock, threatValue), true);
            debounceLockChange.Reset();
        }

        protected override void OnUnitTileChanged(Unit target)
        {
            if (!debounceBodyPull.Expired)
            {
                return;
            }

            AddBodyPullThreat(target);
            debounceBodyPull.Reset();
        }

        protected void AddBodyPullThreat(Unit enemy)
        {
            if (!IsHostile(enemy))
            {
                return;
            }

            BodyPullThreatHelper helper = new BodyPullThreatHelper(this);

            enemy.AcceptVisitor(helper);
        }

        protected override void OnEffectChanged(Effect effect, bool apply)
        {
            base.OnEffectChanged(effect, apply);
        }

        private int CalculateCombatRange()
        {
            double range = (int)ActiveModules
                .Where(m => m.IsRanged)
                .Select(module => module.OptimalRange)
                .Concat(new[] { MaxTargetingRange })
                .Min();
            range *= BestComnatRangeModifier;
            range = Math.Max(3, range);

            return (int)range;
        }

        private int CalculateMaxCombatRange()
        {
            double range = ActiveModules
                .Where(m => m.IsRanged)
                .Select(module => (int)(module.OptimalRange + module.Falloff))
                .Max();
            range = Math.Max(3, range);

            return (int)range;
        }

        public bool IsInAggroRange(Unit target)
        {
            return this is CombatDrone || IsStationary || IsInRangeOf3D(target, AggroRange);
        }

        public virtual void AddThreat(Unit hostile, Threat threat, bool spreadToGroup)
        {
            if (hostile is RemoteControlledCreature)
            {
                threat = Threat.Multiply(threat, 100);
            }

            if (hostile.IsPlayer())
            {
                BossInfo?.OnAggro(hostile as Player);
            }

            ThreatManager.GetOrAddHostile(hostile).AddThreat(threat);
            RemovePseudoThreat(hostile);
            if (!spreadToGroup)
            {
                return;
            }

            ISmartCreatureGroup group = Group;
            if (@group == null)
            {
                return;
            }

            Threat multipliedThreat = Threat.Multiply(threat, 0.5);
            foreach (SmartCreature member in @group.Members)
            {
                if (member == this)
                {
                    continue;
                }

                member.AddThreat(hostile, multipliedThreat, false);
            }
        }

        /// <summary>
        /// This determines if threat can be added to a target based on the following:
        ///  - Is the target already on the threat manager
        ///  - Or is the npc non-passive and the Threat is of some defined type
        /// </summary>
        /// <param name="target">Unit target</param>
        /// <param name="threat">Threat threat</param>
        /// <returns>If the target can be a threat</returns>
        public bool CanAddThreatTo(Unit target, Threat threat)
        {
            return ThreatManager.Contains(target) || (Behavior.Type != BehaviorType.Passive && threat.type != ThreatType.Undefined);
        }

        public void AddAssistThreat(Unit assistant, Unit target, Threat threat)
        {
            if (!ThreatManager.Contains(target))
            {
                return;
            }

            if (!CanAddThreatTo(assistant, threat))
            {
                return;
            }

            AddThreat(assistant, threat, true);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public virtual bool IsFriendly(Unit source)
        {
            return false;
        }

        protected override void OnTileChanged()
        {
            base.OnTileChanged();
            LookingForHostiles();
        }

        protected override void OnEnterZone(IZone zone, ZoneEnterType enterType)
        {
            States.Aggressive = Behavior.Type == BehaviorType.Aggressive;
            base.OnEnterZone(zone, enterType);
            if (this is SentryTurret)
            {
                AI.Push(new SentryTurretCombatAI(this));
            }
            else if (this is IndustrialTurret)
            {
                if ((this as IndustrialTurret).TurretType == TurretType.Mining)
                {
                    AI.Push(new MiningIndustrialTurretAI(this));
                }
                else
                {
                    AI.Push(new HarvestingIndustrialTurretAI(this));
                }
            }
            else if (this is CombatDrone || this is SupportDrone)
            {
                AI.Push(new GuardCombatDroneAI(this));
            }
            else if (this is IndustrialDrone)
            {
                AI.Push(new GuardIndustrialDroneAI(this));
            }
            else if (IsStationary)
            {
                AI.Push(new StationaryIdleAI(this));
            }
            else
            {
                AI.Push(new IdleAI(this));
            }
        }

        protected override void OnRemovedFromZone(IZone zone)
        {
            if (!States.Dead)
            {
                BossInfo?.OnSafeDespawn();
            }

            base.OnRemovedFromZone(zone);
        }

        protected override void OnDamageTaken(Unit source, DamageTakenEventArgs e)
        {
            base.OnDamageTaken(source, e);

            Player player = Zone.ToPlayerOrGetOwnerPlayer(source);
            if (player == null)
            {
                return;
            }

            BossInfo?.OnDamageTaken(this, player);
            if (!IsFriendly(source))
            {
                AddThreat(player, new Threat(ThreatType.Damage, e.TotalDamage * 0.9), true);
            }
        }

        protected override void OnUpdate(TimeSpan time)
        {
            base.OnUpdate(time);
            AI.Update(time);
            UpdatePseudoThreats(time);
        }

        private void RemovePseudoThreat(Unit hostile)
        {
            PseudoThreatManager.Remove(hostile);
        }

        private void UpdatePseudoThreats(TimeSpan time)
        {
            _ = pseudoUpdateFreq.Update(time);
            if (pseudoUpdateFreq.Passed)
            {
                PseudoThreatManager.Update(pseudoUpdateFreq.Elapsed);
                pseudoUpdateFreq.Reset();
            }
        }

        private void CallingForHelp()
        {
            if (!CallForHelp)
            {
                return;
            }

            if (!GlobalTimer.IsPassed(ref lastHelpCalled, TimeSpan.FromSeconds(5)))
            {
                return;
            }

            ISmartCreatureGroup group = Group;
            if (group == null)
            {
                return;
            }

            foreach (SmartCreature member in group.Members.Where(flockMember => flockMember != this))
            {
                member.HelpingFor(this);
            }
        }

        private void HelpingFor(SmartCreature caller)
        {
            if (Armor.Ratio(ArmorMax) < CallForHelpArmorThreshold)
            {
                return;
            }

            ThreatManager.Clear();
            foreach (Hostile hostile in caller.ThreatManager.Hostiles)
            {
                AddThreat(hostile.Unit, new Threat(ThreatType.Undefined, hostile.Threat), true);
            }
        }
    }
}
