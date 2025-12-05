using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs
{
    /// <summary>
    /// DTO para agregar un conductor a la empresa del Admin
    /// </summary>
    public class AddDriverDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(50)]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo no es válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[\d\+\-\(\)\s]{7,}$", ErrorMessage = "Formato de teléfono inválido.")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public required string Password { get; set; }
    }

    /// <summary>
    /// DTO para respuesta de conductor
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
        public DateTime? CreatedAt { get; set; }
    }
}

