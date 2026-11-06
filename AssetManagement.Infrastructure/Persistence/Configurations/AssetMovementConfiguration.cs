using AssetManagement.Domain.Entities.AssetMovements;
using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    internal sealed class AssetMovementConfiguration
        : IEntityTypeConfiguration<AssetMovement>
    {
        public void Configure(
            EntityTypeBuilder<AssetMovement> builder)
        {
            builder.ToTable("AssetMovements");

            builder.HasKey(assetMovement => assetMovement.Id);

            builder.Property(assetMovement => assetMovement.EmployeeNumber)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(assetMovement => assetMovement.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(assetMovement => assetMovement.CreatedAtUtc)
                .IsRequired();

            builder.HasOne<Asset>()
                .WithMany()
                .HasForeignKey(assetMovement => assetMovement.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Store>()
                .WithMany()
                .HasForeignKey(assetMovement => assetMovement.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(assetMovement => new
            {
                assetMovement.AssetId,
                assetMovement.CreatedAtUtc
            });

            builder.HasIndex(assetMovement => new
            {
                assetMovement.StoreId,
                assetMovement.CreatedAtUtc
            });

            builder.HasIndex(assetMovement => new
            {
                assetMovement.EmployeeNumber,
                assetMovement.CreatedAtUtc
            });
        }
    }
}