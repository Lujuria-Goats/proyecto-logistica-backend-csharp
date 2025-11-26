using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public required string Password { get; set; }
    }
}
