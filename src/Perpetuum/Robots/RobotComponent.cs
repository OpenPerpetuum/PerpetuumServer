using Perpetuum.Containers;
using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Perpetuum.Robots
{
    public class RobotHead : RobotComponent
    {
        public RobotHead(IExtensionReader extensionReader) : base(RobotComponentType.Head, extensionReader)
        {
        }
    }

    public class RobotChassis : RobotComponent
    {
        public RobotChassis(IExtensionReader extensionReader) : base(RobotComponentType.Chassis, extensionReader)
        {
        }
    }

    public class RobotLeg : RobotComponent
    {
        public RobotLeg(IExtensionReader extensionReader) : base(RobotComponentType.Leg, extensionReader)
        {
        }
    }

    public abstract class RobotComponent : Item
    {
        private readonly IExtensionReader _extensionReader;

        protected RobotComponent(RobotComponentType type, IExtensionReader extensionReader)
        {
            Type = type;
            _extensionReader = extensionReader;
        }

        public override void Initialize()
        {
            InitModules();
            base.Initialize();
        }

        private Lazy<IEnumerable<Module>> _modules;
        private Lazy<IEnumerable<ActiveModule>> _activeModules;

        private void InitModules()
        {
            _modules = new Lazy<IEnumerable<Module>>(() => Children.OfType<Module>().ToArray());
            _activeModules = new Lazy<IEnumerable<ActiveModule>>(() => Modules.OfType<ActiveModule>().ToArray());
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        public ExtensionBonus[] ExtensionBonuses => _extensionReader.GetRobotComponentExtensionBonus(Definition);

        [CanBeNull]
        public Module GetModule(int slot)
        {
            return Modules.FirstOrDefault(m => m.Slot == slot);
        }

        public Robot ParentRobot => (Robot)ParentEntity;

        public RobotComponentType Type { get; }

        public string ComponentName => Type.ToString().ToLower();

        public IEnumerable<Module> Modules => _modules.Value;

        public IEnumerable<ActiveModule> ActiveModules => _activeModules.Value;

        public void Update(TimeSpan time)
        {
            foreach (ActiveModule activeModule in ActiveModules)
            {
                activeModule.Update(time);
            }
        }

        private long GetSlotFlagMask(int slot)
        {
            return ED.Options.SlotFlags[slot - 1];
        }

        public int MaxSlots => ED.Options.SlotFlags.Length;

        private bool IsValidModuleSlot(int slot)
        {
            return slot > 0 && slot <= MaxSlots;
        }

        private bool IsUsedSlot(int slot)
        {
            foreach (Module module in Modules)
            {
                if (module.Slot == slot)
                {
                    return true;
                }
            }

            return false;
        }

        public void MakeSlotFree(int slot, Container targetContainer)
        {
            Module module = GetModule(slot);
            module?.Unequip(targetContainer);
        }

        public bool CheckUniqueModule(Module module)
        {
            if (ParentRobot == null)
            {
                return true;
            }

            return !module.ED.CategoryFlags.IsUniqueCategoryFlags(out CategoryFlags uniqueCategoryFlag)
|| ParentRobot.FindModuleByCategoryFlag(uniqueCategoryFlag) == null;
        }

        public bool IsValidSlotTo(Module module, int slot)
        {
            if (!IsValidModuleSlot(slot))
            {
                return false;
            }

            long slotFlagMask = GetSlotFlagMask(slot);
            long moduleFlagMask = module.ModuleFlag;
            return (moduleFlagMask & slotFlagMask) == moduleFlagMask;
        }

        public ErrorCodes CanEquipModule(Module module, int slot)
        {
            if (IsUsedSlot(slot))
            {
                return ErrorCodes.UsedSlot;
            }

            if (!IsValidSlotTo(module, slot))
            {
                return ErrorCodes.InvalidSlot;
            }

            if (module.Quantity <= 0)
            {
                return ErrorCodes.WTFErrorMedicalAttentionSuggested;
            }

            return module.IsDamaged
                ? ErrorCodes.ItemHasToBeRepaired
                : !CheckUniqueModule(module) ? ErrorCodes.OnlyOnePerCategoryPerRobotAllowed : ErrorCodes.NoError;
        }

        public void EquipModuleOrThrow(Module module, int slot)
        {
            _ = CanEquipModule(module, slot).ThrowIfError();
            EquipModule(module, slot);
        }

        public void EquipModule(Module module, int slot)
        {
            if (module == null)
            {
                return;
            }

            module.Owner = Owner;
            module.IsRepackaged = false;

            AddChild(module);
            module.Slot = slot;
        }

        private ErrorCodes CanChangeModule(int sourceSlot, int targetSlot)
        {
            Module sourceModule = GetModule(sourceSlot);
            if (sourceModule != null && !IsValidSlotTo(sourceModule, targetSlot))
            {
                return ErrorCodes.InvalidSlot;
            }

            Module targetModule = GetModule(targetSlot);
            return targetModule != null && !IsValidSlotTo(targetModule, sourceSlot) ? ErrorCodes.InvalidSlot : ErrorCodes.NoError;
        }

        public void ChangeModuleOrThrow(int sourceSlot, int targetSlot)
        {
            _ = CanChangeModule(sourceSlot, targetSlot).ThrowIfError();
            ChangeModule(sourceSlot, targetSlot);
        }

        private void ChangeModule(int sourceSlot, int targetSlot)
        {
            Module sourceModule = GetModule(sourceSlot);
            Module targetModule = GetModule(targetSlot);

            if (sourceModule != null)
            {
                sourceModule.Slot = targetSlot;
            }

            if (targetModule != null)
            {
                targetModule.Slot = sourceSlot;
            }
        }

        public override Dictionary<string, object> ToDictionary()
        {
            Dictionary<string, object> result = base.ToDictionary();
            result.Add(k.modules, Modules.ToDictionary("m", m => m.ToDictionary()));
            return result;
        }

        public static new RobotComponent GetOrThrow(long componentEid)
        {
            return (RobotComponent)Repository.LoadOrThrow(componentEid);
        }

        public override ItemPropertyModifier GetPropertyModifier(AggregateField field)
        {
            ItemPropertyModifier modifier = base.GetPropertyModifier(field);
            IEnumerable<Module> modifyingModules = Modules.Where(m => !m.Properties.Any(p => p.Field == field));

            foreach (Module module in modifyingModules)
            {
                ItemPropertyModifier m = module.GetBasePropertyModifier(field);
                m.Modify(ref modifier);
            }

            return modifier;
        }

        public override void UpdateAllProperties()
        {
            foreach (Module module in Modules)
            {
                module.UpdateAllProperties();
            }

            base.UpdateAllProperties();
        }

        public override void UpdateRelatedProperties(AggregateField field)
        {
            foreach (Module module in Modules)
            {
                module.UpdateRelatedProperties(field);
            }

            base.UpdateRelatedProperties(field);
        }
    }
}
