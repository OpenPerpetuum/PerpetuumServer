namespace Perpetuum.Zones.NpcSystem.IndustrialTargetsManagement
{
    public struct IndustrialValue
    {
        public const double CommonMineral = 10;
        public const double RareMineral = 25;
        public const double CommonPlant = 10;
        public const double RarePlant = 25;
        public readonly double value;

        public IndustrialValue(double value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return $"Industrial value: {value}";
        }
    }
}
