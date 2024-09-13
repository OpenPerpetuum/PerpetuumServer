using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules.ModuleProperties;
using Perpetuum.Units;
using Perpetuum.Zones;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using System;

namespace Perpetuum.Modules
{
    public class EnergyTransfererModule : ActiveModule
    {
        private readonly ItemProperty _energyTransferAmount;

        public EnergyTransfererModule() : base(true)
        {
            _energyTransferAmount = new ModuleProperty(this, AggregateField.energy_transfer_amount);
            AddProperty(_energyTransferAmount);
        }

        protected override void OnAction()
        {
            UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            (ParentIsPlayer() && unitLock.Target is Npc).ThrowIfTrue(ErrorCodes.ThisModuleIsNotSupportedOnNPCs);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            double coreAmount = _energyTransferAmount.Value;

            coreAmount = ModifyValueByOptimalRange(unitLock.Target, coreAmount);

            double coreNeutralized = 0.0;
            double coreTransfered = 0.0;

            if (coreAmount > 0.0)
            {
                double core = ParentRobot.Core;

                ParentRobot.Core -= coreAmount;
                coreNeutralized = Math.Abs(core - ParentRobot.Core);

                double targetCore = unitLock.Target.Core;

                unitLock.Target.Core += coreNeutralized;
                coreTransfered = Math.Abs(targetCore - unitLock.Target.Core);
                unitLock.Target.SpreadAssistThreatToNpcs(ParentRobot, new Threat(ThreatType.Support, coreAmount * 2));
            }

            CombatLogPacket packet = new CombatLogPacket(CombatLogType.EnergyTransfer, unitLock.Target, ParentRobot, this);

            packet.AppendDouble(coreAmount);
            packet.AppendDouble(coreNeutralized);
            packet.AppendDouble(coreTransfered);
            packet.Send(unitLock.Target, ParentRobot);
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }
    }
}