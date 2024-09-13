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
    public abstract class RobotComponent : Item
    {
        private readonly IExtensionReader extensionReader;
        private Lazy<IEnumerable<Module>> modules;
        private Lazy<IEnumerable<ActiveModule>> activeModules;

        protected RobotComponent(RobotComponentType type, IExtensionReader extensionReader)
        {
            Type = type;
            this.extensionReader = extensionReader;
        }

        public ExtensionBonus[] ExtensionBonuses => extensionReader.GetRobotComponentExtensionBonus(Definition);

        public Robot ParentRobot => (Robot)ParentEntity;

        public RobotComponentType Type { get; }

        public string ComponentName => Type.ToString().ToLower();

        public IEnumerable<Module> Modules => modules.Value;

        public IEnumerable<ActiveModule> ActiveModules => activeModules.Value;

        public int MaxSlots => ED.Options.SlotFlags.Length;

        public override void Initialize()
        {
            InitModules();
            base.Initialize();
        }

        private void InitModules()
        {
            modules = new Lazy<IEnumerable<Module>>(() => Children.OfType<Module>().ToArray());
            activeModules = new Lazy<IEnumerable<ActiveModule>>(() => Modules.OfType<ActiveModule>().ToArray());
        }

        public override void AcceptVisitor(IEntityVisitor visitor)
        {
            if (!TryAcceptVisitor(this, visitor))
            {
                base.AcceptVisitor(visitor);
            }
        }

        [CanBeNull]
        public Module GetModule(int slot)
        {
            return Modules.FirstOrDefault(m => m.Slot == slot);
        }

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
            return ParentRobot == null ||
                !module.ED.CategoryFlags.IsUniqueCategoryFlags(out CategoryFlags uniqueCategoryFlag) ||
                ParentRobot.FindModuleByCategoryFlag(uniqueCategoryFlag) == null;
        }

        public bool IsValidSlotTo(Module module, int slot)
        {
            if (!IsValidModuleSlot(slot))
            {
                return false;
            }

            long slotFlagMask = GetSlotFlagMask(slot);
            long moduleFlagMask = module.ModuleFlag;
            long specializedFlag = (long)Math.Pow(2, (double)SlotFlags.specialized);
            bool specializedSlot = (slotFlagMask & specializedFlag) == specializedFlag;
            bool specializedModule = (moduleFlagMask & specializedFlag) == specializedFlag;

            return (moduleFlagMask & slotFlagMask) == moduleFlagMask &&
                (!specializedSlot || specializedModule);
        }

        public ErrorCodes CanEquipModule(Module module, int slot)
        {
            return IsUsedSlot(slot)
                ? ErrorCodes.UsedSlot
                : !IsValidSlotTo(module, slot)
                    ? ErrorCodes.InvalidSlot
                    : module.Quantity <= 0
                        ? ErrorCodes.WTFErrorMedicalAttentionSuggested
                        : module.IsDamaged
                            ? ErrorCodes.ItemHasToBeRepaired
                            : !CheckUniqueModule(module)
                                ? ErrorCodes.OnlyOnePerCategoryPerRobotAllowed
                                : ErrorCodes.NoError;
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

            return targetModule != null && !IsValidSlotTo(targetModule, sourceSlot)
                ? ErrorCodes.InvalidSlot
                : ErrorCodes.NoError;
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
