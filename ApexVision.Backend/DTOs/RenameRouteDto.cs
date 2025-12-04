using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApexVision.Backend.DTOs
{
    public class RenameRouteDto
    {
        [Required(ErrorMessage = "El nuevo nombre es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        [JsonPropertyName("newName")]
        public required string NewName { get; set; }
    }
}

