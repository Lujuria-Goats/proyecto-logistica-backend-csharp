﻿using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs.Auth
{
    public class LoginDto
    {
        /// <summary>
        /// Email, nombre de usuario o número de teléfono
        /// </summary>
        [Required(ErrorMessage = "El correo electrónico, nombre de usuario o teléfono es obligatorio.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public required string Password { get; set; }
    }
}
