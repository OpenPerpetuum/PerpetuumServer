using System;

namespace Perpetuum.Modules
{
    /// Doc - These are powers of 2
    /// <summary>
    /// Types for module slots
    /// </summary>
    [Flags]
    public enum SlotFlags
    {
        turret = 1,
        missile = 2,
        melee = 4,
        head = 8,
        chassis = 16,
        leg = 32,
        small = 64,
        medium = 128,
        large = 256,
        industrial = 512,
        ew_and_engineering = 1024
    }
}