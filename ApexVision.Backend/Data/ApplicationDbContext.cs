using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApexVision.Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, Role, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the relationship between User (Driver) and Order
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.Driver)
                .HasForeignKey(o => o.DriverId)
                .OnDelete(DeleteBehavior.SetNull); // If a driver is deleted, the order's DriverId is set to null

            // Seed a default admin user for testing
            var adminUser = new User
            {
                Id = 1,
                FullName = "Admin User",
                Email = "admin@apexvision.com",
                UserName = "admin@apexvision.com", // Set UserName for Identity
                PhoneNumber = "+1234567890", // Add a default phone number
                NormalizedEmail = "ADMIN@APEXVISION.COM",
                NormalizedUserName = "ADMIN@APEXVISION.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var passwordHasher = new PasswordHasher<User>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "Admin123!");

            modelBuilder.Entity<User>().HasData(adminUser);

            // Seed a default admin role
            var adminRole = new Role
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            modelBuilder.Entity<Role>().HasData(adminRole);

            // Assign admin role to the admin user
            modelBuilder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int>
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                }
            );
        }
    }
}
