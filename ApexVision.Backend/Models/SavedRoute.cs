using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexVision.Backend.Models
{
    public class SavedRoute
    {
        public int Id { get; set; }

        [Required]
        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public virtual User? Driver { get; set; }

        [Required]
        [MaxLength(100)]
        public required string RouteName { get; set; }

        [Required]
        public required string OrderIds { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastUsedDate { get; set; }

        public bool IsActive { get; set; } = true;

        public int? OptimizationScore { get; set; }

        /// <summary>
        /// ID del Admin que asignó esta ruta al conductor (null si el conductor la creó él mismo)
        /// </summary>
        public int? AssignedByAdminId { get; set; }

        [ForeignKey("AssignedByAdminId")]
        public virtual User? AssignedByAdmin { get; set; }
    }
}

