using Microsoft.EntityFrameworkCore;
using AssetManagement.Domain.Entities.Employees;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Vendors;

namespace AssetManagement.Infrastructure.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<Asset> Assets => Set<Asset>();
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<AssetMovement> AssetMovement => Set<AssetMovement>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Contact> Contacts => Set<Contact>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
