﻿using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ApexVision.Backend.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
        
        /// <summary>
        /// NIT o Cédula de la empresa (solo para Admin)
        /// </summary>
        [MaxLength(20)]
        public string? CompanyNit { get; set; }
        
        /// <summary>
        /// Nombre de la empresa (solo para Admin)
        /// </summary>
        [MaxLength(150)]
        public string? CompanyName { get; set; }
        
        /// <summary>
        /// Admins que han vinculado a este driver (si es Driver)
        /// </summary>
        public ICollection<AdminDriver> LinkedToAdmins { get; set; } = new List<AdminDriver>();
        
        /// <summary>
        /// Drivers vinculados a este admin (si es Admin)
        /// </summary>
        public ICollection<AdminDriver> LinkedDrivers { get; set; } = new List<AdminDriver>();
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<SavedRoute> SavedRoutes { get; set; } = new List<SavedRoute>();
    }
}
