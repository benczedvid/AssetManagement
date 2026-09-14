using AssetManagement.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the <see cref="Asset"/> entity.
/// </summary>
internal sealed class AssetConfiguration
    : IEntityTypeConfiguration<Asset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(asset => asset.Id);
        builder.Property(asset => asset.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(asset => asset.AssetName).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.AssetType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(asset => asset.AssetStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(asset => asset.Manufacturer).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.Model).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.SerialNumber).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.MacAddress).HasMaxLength(17).IsRequired(false);
        builder.Property(asset => asset.WiFiMacAddress).HasMaxLength(17).IsRequired(false);
        builder.Property(asset => asset.Imei).HasMaxLength(15).IsRequired(false);
        builder.Property(asset => asset.OperatingSystem).HasMaxLength(100).IsRequired(false);
        builder.Property(asset => asset.OperatingSystemVersion).HasMaxLength(100).IsRequired(false);
        builder.Property(asset => asset.AssignedStoreId).IsRequired();
        builder.Property(asset => asset.AssignedUserId).IsRequired(false);
        builder.Property(asset => asset.AssignedEmployeeNumber).HasMaxLength(10).IsRequired(false);
        builder.Property(asset => asset.CreatedAtUtc).IsRequired(true);
        builder.Property(asset => asset.AssignedVendorId).IsRequired(false);
        builder.Property(asset => asset.RfidTagId).IsRequired(false);
    }

    private static void ConfigureIndexes(
        EntityTypeBuilder<Asset> builder)
    {
        builder.HasIndex(asset => asset.SerialNumber)
            .IsUnique()
            .HasDatabaseName("UX_Assets_SerialNumber");

        builder.HasIndex(asset => asset.AssignedStoreId)
            .HasDatabaseName("IX_Assets_AssignedStoreId");

        builder.HasIndex(asset => asset.AssignedUserId)
            .HasDatabaseName("IX_Assets_AssignedUserId");

        builder.HasIndex(asset => asset.AssignedVendorId)
            .HasDatabaseName("IX_Assets_AssignedVendorId");
    }

    private static void ConfigureRelationships(
        EntityTypeBuilder<Asset> builder)
    {
        builder.HasOne(asset => asset.AssignedStore)
            .WithMany()
            .HasForeignKey(asset => asset.AssignedStoreId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(asset => asset.AssignedUser)
            .WithMany()
            .HasForeignKey(asset => asset.AssignedUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(asset => asset.AssignedVendor)
            .WithMany()
            .HasForeignKey(Asset => Asset.AssignedVendorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}