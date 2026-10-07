namespace MotorCity.Vehicle
{
    /// <summary>
    /// Stable gameplay/save identifiers for player vehicles.
    ///
    /// These are IDs, not model names. Future vehicle model replacement can
    /// keep the same ID and therefore preserve saves, audio/customization
    /// profiles and progression without scattering string literals.
    /// </summary>
    public static class VehicleIds
    {
        public const string Beatall = "beatall";
        public const string Street = "street";
        public const string Peugeot306 = "peugeot306";
        public const string ToyotaAE86 = "toyotaae86";
        public const string Hybrid = "hybrid";
        public const string Porsche996 = "porsche996";
        public const string AmgGT = "amggt";
        public const string Camaro = "camaro";
        public const string Delorean = "delorean";
        public const string Bus = "bus";

        public const string Default = Beatall;
    }
}
