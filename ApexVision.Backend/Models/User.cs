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
        /// ID del Admin que agregó este conductor (solo para Drivers)
        /// </summary>
        public int? AdminId { get; set; }
        
        /// <summary>
        /// Admin que gestiona este conductor
        /// </summary>
        public User? Admin { get; set; }
        
        /// <summary>
        /// Conductores que pertenecen a este Admin
        /// </summary>
        public ICollection<User> Drivers { get; set; } = new List<User>();
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<SavedRoute> SavedRoutes { get; set; } = new List<SavedRoute>();
    }
}
