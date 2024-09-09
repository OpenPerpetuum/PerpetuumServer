using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Modules.Weapons;
using Perpetuum.Robots;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Beams;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.RemoteControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perpetuum.Modules
{
    public class ScorcherModule : EnergyDispersionModule
    {
        private const int AffectedTargetsDepth = 5;
        private const double BouncingDistance = 7.5;
        private readonly ItemProperty electricDamage;

        public ScorcherModule()
        {
            electricDamage = new ModuleProperty(this, AggregateField.electric_damage);
            AddProperty(electricDamage);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        protected override void OnAction()
        {
            UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            List<Unit> affectedTargets = new List<Unit> { unitLock.Target };
            GetAffectedTargetsRecursively(affectedTargets, unitLock.Target, AffectedTargetsDepth);
            Robot chainedRobot = ParentRobot;
            double chainedDamageModifier = 1.0;

            foreach (Unit target in affectedTargets)
            {
                if (chainedRobot != ParentRobot &&
                    new UnitVisibility(chainedRobot, target).GetLineOfSight(false).hit)
                {
                    break;
                }

                BeamBuilder deployBeamBuilder = Beam.NewBuilder()
                    .WithType(BeamType.medium_e_nezt_beam)
                    .WithSource(chainedRobot)
                    .WithTarget(target)
                    .WithState(BeamState.Hit)
                    .WithDuration(TimeSpan.FromSeconds(5));
                Zone.CreateBeam(deployBeamBuilder);
                double coreNeutralized = electricDamage.Value * chainedDamageModifier;
                double coreNeutralizedDone = 0.0;
                ModifyValueByReactorRadiation(target, ref coreNeutralized);
                coreNeutralized = ModifyValueByOptimalRange(chainedRobot, target, coreNeutralized);
                if (coreNeutralized > 0.0)
                {
                    double core = target.Core;

                    target.Core -= coreNeutralized;
                    coreNeutralizedDone = Math.Abs(core - target.Core);
                    target.OnCombatEvent(ParentRobot, new EnergyDispersionEventArgs(coreNeutralizedDone));

                    double threatValue = (coreNeutralizedDone / 2) + 1;

                    target.AddThreat(ParentRobot, new Threat(ThreatType.EnWar, threatValue));
                }

                IDamageBuilder builder = GetDamageBuilder(coreNeutralizedDone);
                _ = Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => target.TakeDamage(builder.Build()));
                CombatLogPacket packet = new CombatLogPacket(CombatLogType.EnergyNeutralize, target, ParentRobot, this);
                packet.AppendDouble(coreNeutralized);
                packet.AppendDouble(coreNeutralizedDone);
                packet.Send(target, ParentRobot);
                chainedRobot = (Robot)target;
                chainedDamageModifier -= 0.15;
            }
        }

        private void GetAffectedTargetsRecursively(List<Unit> targets, Unit lastTarget, int depth)
        {
            if (depth <= 0)
            {
                return;
            }

            Unit newTarget = lastTarget.Zone
                .GetUnitsWithinRange2D(lastTarget.CurrentPosition, BouncingDistance)
                .OfType<Robot>()
                .Where(x =>
                    x != ParentRobot &&
                    !targets.Any(y => y.Eid == x.Eid) &&
                    (!(ParentRobot is Npc) || x.IsPlayer() || (x is RemoteControlledCreature)))
                .GetNearestUnit(lastTarget.CurrentPosition);
            if (newTarget != null)
            {
                targets.Add(newTarget);
                GetAffectedTargetsRecursively(targets, newTarget, depth - 1);
            }
        }

        private IDamageBuilder GetDamageBuilder(double damageValue)
        {
            return DamageInfo.Builder
                .WithAttacker(ParentRobot)
                .WithOptimalRange(OptimalRange)
                .WithFalloff(Falloff)
                .WithDamages(new[] { new Damage(DamageType.Electric, damageValue) });
        }
    }
}
