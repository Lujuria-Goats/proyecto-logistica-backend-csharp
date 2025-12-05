﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexVision.Backend.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public required string Description { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [Required]
        public required string Address { get; set; }

        public OrderStatus Status { get; set; }

        public bool RequiresEvidence { get; set; }

        /// <summary>
        /// ID del Admin que creó el pedido
        /// </summary>
        public int? AdminId { get; set; }
        
        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }

        public int? DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual User? Driver { get; set; }

        public string? EvidenceUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
