using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public required string Password { get; set; }
        
        public required string Role { get; set; } = "Driver"; // Default role as string
    }
}
