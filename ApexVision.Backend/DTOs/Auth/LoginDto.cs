﻿using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs.Auth
{
    public class LoginDto
    {
        /// <summary>
        /// Identificador: puede ser email, username, teléfono o NIT/documento
        /// </summary>
        [Required(ErrorMessage = "El identificador es obligatorio (email, usuario, teléfono o documento).")]
        public required string Identifier { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public required string Password { get; set; }
    }
}
