using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ApexVision.Backend.Models
{
    public class User : IdentityUser<int>
    {
        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
