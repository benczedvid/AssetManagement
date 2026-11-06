using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="PackageEvent"/> entity.
/// </summary>
internal sealed class PackageEventConfiguration : IEntityTypeConfiguration<PackageEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PackageEvent> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<PackageEvent> builder)
    {
        builder.ToTable("PackageEvents");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<PackageEvent> builder)
    {
        builder.HasKey(packageEvent => packageEvent.Id);
        builder.Property(packageEvent => packageEvent.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<PackageEvent> builder)
    {
        builder.Property(packageEvent => packageEvent.PackageId).IsRequired();
        builder.Property(packageEvent => packageEvent.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(packageEvent => packageEvent.PreviousStatus).HasConversion<string>().HasMaxLength(50).IsRequired(false);
        builder.Property(packageEvent => packageEvent.NewStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(packageEvent => packageEvent.RelatedAssetId).IsRequired(false);
        builder.Property(packageEvent => packageEvent.RelatedUserId).IsRequired(false);
        builder.Property(packageEvent => packageEvent.Comment).HasMaxLength(2000).IsRequired(false);
        builder.Property(packageEvent => packageEvent.PerformedByUserId).IsRequired();
        builder.Property(packageEvent => packageEvent.PerformedByUserName).HasMaxLength(250).IsRequired();
        builder.Property(packageEvent => packageEvent.OccurredAtUtc).IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<PackageEvent> builder)
    {
        builder.HasIndex(packageEvent => packageEvent.PackageId).HasDatabaseName("IX_PackageEvents_PackageId");
        builder.HasIndex(packageEvent => packageEvent.Type).HasDatabaseName("IX_PackageEvents_Type");
        builder.HasIndex(packageEvent => packageEvent.RelatedAssetId).HasDatabaseName("IX_PackageEvents_RelatedAssetId");
        builder.HasIndex(packageEvent => packageEvent.RelatedUserId).HasDatabaseName("IX_PackageEvents_RelatedUserId");
        builder.HasIndex(packageEvent => packageEvent.PerformedByUserId).HasDatabaseName("IX_PackageEvents_PerformedByUserId");
        builder.HasIndex(packageEvent => packageEvent.OccurredAtUtc).HasDatabaseName("IX_PackageEvents_OccurredAtUtc");
        builder.HasIndex(packageEvent => new
            {
                packageEvent.PackageId,
                packageEvent.OccurredAtUtc
            })
            .HasDatabaseName("IX_PackageEvents_PackageId_OccurredAtUtc");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<PackageEvent> builder)
    {
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(packageEvent => packageEvent.RelatedAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(packageEvent => packageEvent.RelatedUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(packageEvent => packageEvent.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}