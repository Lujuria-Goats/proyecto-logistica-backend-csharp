namespace ApexVision.Backend.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public string Status { get; set; } // Pending, Completed, etc.
        public string EvidenceUrl { get; set; }
        public int? DriverId { get; set; }
        public string DriverName { get; set; } // Nombre del chofer para mostrarlo en el mapa
    }
}