using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    public class UpdateRouteDto
    {
        [Required(ErrorMessage = "El nombre de la ruta es obligatorio.")]
        [MaxLength(100)]
        public required string RouteName { get; set; }

        [Required(ErrorMessage = "Los IDs de pedidos son obligatorios.")]
        public required List<int> OrderIds { get; set; }
    }
}
