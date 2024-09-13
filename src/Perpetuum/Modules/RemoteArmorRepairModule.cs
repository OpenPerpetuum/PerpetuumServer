using Perpetuum.EntityFramework;
using Perpetuum.Units;
using Perpetuum.Zones.Locking.Locks;
using Perpetuum.Zones.NpcSystem;
using Perpetuum.Zones.NpcSystem.ThreatManaging;
using Perpetuum.Zones.PBS;

namespace Perpetuum.Modules
{
    public sealed class RemoteArmorRepairModule : ArmorRepairerBaseModule
    {
        public RemoteArmorRepairModule() : base(true)
        {
        }

        protected override void OnAction()
        {
            UnitLock unitLock = GetLock().ThrowIfNotType<UnitLock>(ErrorCodes.InvalidLockType);

            (ParentIsPlayer() && unitLock.Target is Npc).ThrowIfTrue(ErrorCodes.ThisModuleIsNotSupportedOnNPCs);
            // csak pbs-re nezzuk
            ((unitLock.Target is IPBSObject) && unitLock.Target.Armor.Ratio(unitLock.Target.ArmorMax) >= 1.0).ThrowIfTrue(ErrorCodes.BuildingIsAtFullHealth);

            if (!LOSCheckAndCreateBeam(unitLock.Target))
            {
                OnError(ErrorCodes.LOSFailed);

                return;
            }

            double repairAmount = armorRepairAmount.Value;

            repairAmount = ModifyValueByOptimalRange(unitLock.Target, repairAmount);
            OnRepair(unitLock.Target, repairAmount);
            unitLock.Target.SpreadAssistThreatToNpcs(ParentRobot, new Threat(ThreatType.Support, repairAmount * 2));
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