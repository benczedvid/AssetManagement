using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    public sealed class ApplicationUserConfiguration
        : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("ApplicationUsers");

            builder.HasKey(user => user.Id);
            builder.Property(user => user.Id).ValueGeneratedNever();
            builder.Property(user => user.EntraObjectId).IsRequired();
            builder.Property(user => user.EntraTenantId).IsRequired();
            builder.HasIndex(user => new
            {
                user.EntraTenantId,
                user.EntraObjectId
            }).IsUnique();
            builder.Property(user => user.FirstName).HasMaxLength(100);
            builder.Property(user => user.LastName).HasMaxLength(100);
            builder.Property(user => user.DisplayName).IsRequired().HasMaxLength(200);
            builder.Property(user => user.Mail).HasMaxLength(320);
            builder.Property(user => user.Department).HasMaxLength(200);
            builder.Property(user => user.JobTitle).HasMaxLength(200);
            builder.Property(user => user.MobilePhone).HasMaxLength(50);
            builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(user => user.StoreId).IsRequired(false);
            builder.Property(user => user.IsActive).IsRequired();
            builder.Property(user => user.LastLoginAtUtc);
            builder.HasOne<Store>()
                .WithMany()
                .HasForeignKey(user => user.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(user => user.StoreId);
        }
    }
}