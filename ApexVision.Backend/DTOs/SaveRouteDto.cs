using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class SaveRouteDto
    {
        [Required(ErrorMessage = "El nombre de la ruta es obligatorio.")]
        [MaxLength(100)]
        public required string RouteName { get; set; }

        [Required(ErrorMessage = "Los IDs de pedidos son obligatorios.")]
        public required List<int> OrderIds { get; set; }

        public int? DriverId { get; set; } // Opcional: Solo para cuando el Admin guarda la ruta para un conductor
    }

    public class SavedRouteDto
    {
        public int Id { get; set; }

        public required string RouteName { get; set; }

        public List<int> OrderIds { get; set; } = new();

        public DateTime CreatedDate { get; set; }

        public DateTime? LastUsedDate { get; set; }

        public bool IsActive { get; set; }

        public int? OptimizationScore { get; set; }
    }

    public class LoadSavedRouteDto
    {
        public int SavedRouteId { get; set; }

        public required string PhoneNumber { get; set; } // Para validar que es el conductor
    }
}

