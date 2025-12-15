using ApexVision.Backend.Models;

namespace ApexVision.Backend.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
        public OrderStatus Status { get; set; }
        public bool RequiresEvidence { get; set; }
        public int? DriverId { get; set; }
        public string? EvidenceUrl { get; set; }
        public int? StopOrder { get; set; }
        public int? StopNumber { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
