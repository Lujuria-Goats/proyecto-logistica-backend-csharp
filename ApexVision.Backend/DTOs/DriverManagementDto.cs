using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    /// <summary>
    /// DTO para respuesta de conductor vinculado
    /// </summary>
    public class DriverResponseDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public DateTime? LinkedAt { get; set; }
    }
}

