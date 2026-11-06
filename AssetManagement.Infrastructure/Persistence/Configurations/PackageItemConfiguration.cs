using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="PackageItem"/> entity.
/// </summary>
internal sealed class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<PackageItem> builder)
    {
        builder.ToTable("PackageItems");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<PackageItem> builder)
    {
        builder.HasKey(packageItem => packageItem.Id);
        builder.Property(packageItem => packageItem.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<PackageItem> builder)
    {
        builder.Property(packageItem => packageItem.PackageId).IsRequired();
        builder.Property(packageItem => packageItem.AssetId).IsRequired();
        builder.Property(packageItem => packageItem.AddedAtUtc).IsRequired();
        builder.Property(packageItem => packageItem.AddedByUserId).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<PackageItem> builder)
    {
        builder.HasIndex(packageItem => packageItem.PackageId).HasDatabaseName("IX_PackageItems_PackageId");
        builder.HasIndex(packageItem => packageItem.AssetId).HasDatabaseName("IX_PackageItems_AssetId");
        builder.HasIndex(packageItem => packageItem.AddedByUserId).HasDatabaseName("IX_PackageItems_AddedByUserId");
        builder.HasIndex(packageItem => new
            {
                packageItem.PackageId,
                packageItem.AssetId
            })
            .IsUnique()
            .HasDatabaseName("UX_PackageItems_PackageId_AssetId");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<PackageItem> builder)
    {
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(packageItem => packageItem.AssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(packageItem => packageItem.AddedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}