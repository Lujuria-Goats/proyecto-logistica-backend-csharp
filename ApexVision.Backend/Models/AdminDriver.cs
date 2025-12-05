namespace ApexVision.Backend.Models
{
    /// <summary>
    /// Tabla de vinculación entre Admin y Driver (muchos a muchos)
    /// Un driver puede estar vinculado a múltiples admins
    /// </summary>
    public class AdminDriver
    {
        public int AdminId { get; set; }
        public User Admin { get; set; } = null!;
        
        public int DriverId { get; set; }
        public User Driver { get; set; } = null!;
        
        public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    }
}

