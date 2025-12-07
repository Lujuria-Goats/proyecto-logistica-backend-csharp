using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class LinkDriverDto
    {
        [Required(ErrorMessage = "El número de teléfono es obligatorio")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string PhoneNumber { get; set; } = null!;

        // Opcional: permitir buscar por Id si el frontend lo envía
        public int? DriverId { get; set; }
    }
}

