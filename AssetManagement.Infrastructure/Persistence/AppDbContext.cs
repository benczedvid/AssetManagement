using Microsoft.EntityFrameworkCore;
using AssetManagement.Domain.Entities.Employees;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Vendors;
using AssetManagement.Domain.Entities.AssetHistories;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.CaseNumbers;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.ServiceDesk;

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
        public DbSet<AssetHistory> AssetHistory => Set<AssetHistory>();
        public DbSet<ServiceCase> ServiceCases => Set<ServiceCase>();
        public DbSet<ServiceCaseEvent> ServiceCaseEvents => Set<ServiceCaseEvent>();
        public DbSet<CaseNumberSequence> CaseNumberSequences => Set<CaseNumberSequence>();
        public DbSet<Package> Packages => Set<Package>();
        public DbSet<PackageItem> PackageItems => Set<PackageItem>();
        public DbSet<PackageEvent> PackageEvents => Set<PackageEvent>();
        public DbSet<ServiceDeskRequestClosureOutbox> ServiceDeskRequestClosureOutbox => Set<ServiceDeskRequestClosureOutbox>();
        public DbSet<ServiceDeskRequestOutbox> ServiceDeskRequestOutbox  => Set<ServiceDeskRequestOutbox>();
        public DbSet<ServiceDeskReconciliationAudit> ServiceDeskReconciliationAudits => Set<ServiceDeskReconciliationAudit>();
        public DbSet<ServiceDeskNoteOutbox> ServiceDeskNoteOutbox => Set<ServiceDeskNoteOutbox>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
