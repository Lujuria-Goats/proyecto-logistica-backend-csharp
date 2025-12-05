﻿﻿using System.ComponentModel.DataAnnotations;

namespace ApexVision.Backend.DTOs.Auth
{
    /// <summary>
    /// DTO base para registro de usuarios
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(50)]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "El nombre de usuario solo puede contener letras, números y guiones bajos.")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [MaxLength(100)]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "El número de teléfono es obligatorio.")]
        [RegularExpression(@"^[\d\+\-\(\)\s]{7,}$", ErrorMessage = "El formato del número de teléfono no es válido.")]
        public required string PhoneNumber { get; set; }

        /// <summary>
        /// Rol del usuario: "Admin" o "Driver". Por defecto "Driver"
        /// </summary>
        [RegularExpression(@"^(Admin|Driver)$", ErrorMessage = "El rol debe ser 'Admin' o 'Driver'.")]
        public string Role { get; set; } = "Driver";
    }

    /// <summary>
    /// DTO para registro de Admin (incluye datos de empresa)
    /// </summary>
    public class RegisterAdminDto : RegisterDto
    {
        [Required(ErrorMessage = "El NIT o CC de la empresa es obligatorio.")]
        [MaxLength(20)]
        [RegularExpression(@"^[\d\-]+$", ErrorMessage = "El NIT/CC solo puede contener números y guiones.")]
        public required string CompanyNit { get; set; }

        [Required(ErrorMessage = "El nombre de la empresa es obligatorio.")]
        [MaxLength(150)]
        public required string CompanyName { get; set; }
    }

    /// <summary>
    /// DTO para registro de Driver (chofer)
    /// </summary>
    public class RegisterDriverDto : RegisterDto
    {
        // Campos adicionales específicos para drivers si se necesitan en el futuro
    }
}
