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

        public virtual DbSet<Order> Orders { get; set; }

        public virtual DbSet<SavedRoute> SavedRoutes { get; set; }
        
        public virtual DbSet<AdminDriver> AdminDrivers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relación Driver -> Pedidos
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.Driver)
                .HasForeignKey(o => o.DriverId)
                .OnDelete(DeleteBehavior.SetNull); // Si se elimina un conductor, el DriverId del pedido se establece en null

            // Relación Admin -> Pedidos (FALTABA)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Admin)
                .WithMany() // O .WithMany(u => u.OrdersCreated) si tienes esa propiedad en User
                .HasForeignKey(o => o.AdminId)
                .OnDelete(DeleteBehavior.Restrict); // Para no borrar historial si se borra el admin

            // Relación Driver -> SavedRoute
            modelBuilder.Entity<User>()
                .HasMany(u => u.SavedRoutes)
                .WithOne(r => r.Driver)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Cascade); // Si se elimina un conductor, sus rutas guardadas se eliminan
            
            // Relación AdminDriver (muchos a muchos)
            modelBuilder.Entity<AdminDriver>()
                .HasKey(ad => new { ad.AdminId, ad.DriverId });
            
            modelBuilder.Entity<AdminDriver>()
                .HasOne(ad => ad.Admin)
                .WithMany(u => u.LinkedDrivers)
                .HasForeignKey(ad => ad.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<AdminDriver>()
                .HasOne(ad => ad.Driver)
                .WithMany(u => u.LinkedToAdmins)
                .HasForeignKey(ad => ad.DriverId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
