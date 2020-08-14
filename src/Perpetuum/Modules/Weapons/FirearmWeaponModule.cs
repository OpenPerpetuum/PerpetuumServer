using Perpetuum.EntityFramework;
using Perpetuum.ExportedTypes;
using Perpetuum.Items;
using Perpetuum.Units;
using System.Collections.Generic;

namespace Perpetuum.Modules.Weapons
{
    /// <summary>
    /// Special subclass of weapon with special capabilities against plants
    /// applies damage like missiles and doesn't miss.
    /// </summary>
    public class FirearmWeaponModule : WeaponModule
    {
        public ModuleProperty PlantDamageModifier { get; }

        public FirearmWeaponModule(CategoryFlags ammoCategoryFlags) : base(ammoCategoryFlags)
        {
            PlantDamageModifier = new ModuleProperty(this, AggregateField.damage_toxic_modifier);
            AddProperty(PlantDamageModifier);
        }

        protected override bool CheckAccuracy(Unit victim)
        {
            return false;
        }

        protected override IDamageBuilder GetDamageBuilder()
        {
            return base.GetDamageBuilder().WithExplosionRadius(Accuracy.Value);
        }

        protected override IDamageBuilder GetPlantDamageBuilder()
        {
            return GetDamageBuilder().WithPlantDamages(GetPlantDamage());
        }

        private IEnumerable<Damage> GetPlantDamage()
        {
            var ammo = (WeaponAmmo)GetAmmo();
            return ammo != null ? ammo.GetPlantDamage() : new Damage[0];
        }

    }
}
