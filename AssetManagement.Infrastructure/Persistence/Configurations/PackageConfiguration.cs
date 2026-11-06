using AssetManagement.Domain.Entities.Packages;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="Package"/> entity.
/// </summary>
internal sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("Packages");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(package => package.Id);
        builder.Property(package => package.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<Package> builder)
    {
        builder.Property(package => package.PackageNumber).HasMaxLength(50).IsRequired();
        builder.Property(package => package.SourceStoreId).IsRequired();
        builder.Property(package => package.DestinationStoreId).IsRequired();
        builder.Property(package => package.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(package => package.CourierUserId).IsRequired(false);
        builder.Property(package => package.Comment).HasMaxLength(2000).IsRequired(false);
        builder.Property(package => package.CreatedAtUtc).IsRequired();
        builder.Property(package => package.CreatedByUserId).IsRequired();
        builder.Property(package => package.DispatchedAtUtc).IsRequired(false);
        builder.Property(package => package.DispatchedByUserId).IsRequired(false);
        builder.Property(package => package.ReceivedAtUtc).IsRequired(false);
        builder.Property(package => package.ReceivedByUserId).IsRequired(false);
        builder.Property(package => package.CancelledAtUtc).IsRequired(false);
        builder.Property(package => package.CancelledByUserId).IsRequired(false);
        builder.Property(package => package.CourierUserId).IsRequired(false);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<Package> builder)
    {
        builder.HasIndex(package => package.PackageNumber).IsUnique().HasDatabaseName("UX_Packages_PackageNumber");
        builder.HasIndex(package => package.Status).HasDatabaseName("IX_Packages_Status");
        builder.HasIndex(package => package.SourceStoreId).HasDatabaseName("IX_Packages_SourceStoreId");
        builder.HasIndex(package => package.DestinationStoreId).HasDatabaseName("IX_Packages_DestinationStoreId");
        builder.HasIndex(package => package.CourierUserId).HasDatabaseName("IX_Packages_CourierUserId");
        builder.HasIndex(package => package.CreatedByUserId).HasDatabaseName("IX_Packages_CreatedByUserId");
        builder.HasIndex(package => package.DispatchedByUserId).HasDatabaseName("IX_Packages_DispatchedByUserId");
        builder.HasIndex(package => package.ReceivedByUserId).HasDatabaseName("IX_Packages_ReceivedByUserId");
        builder.HasIndex(package => package.CancelledByUserId).HasDatabaseName("IX_Packages_CancelledByUserId");
        builder.HasIndex(package => new
            {
                package.SourceStoreId,
                package.Status
            })
            .HasDatabaseName("IX_Packages_SourceStoreId_Status");

        builder.HasIndex(package => new
            {
                package.DestinationStoreId,
                package.Status
            })
            .HasDatabaseName("IX_Packages_DestinationStoreId_Status");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<Package> builder)
    {
        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(package => package.SourceStoreId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(package => package.DestinationStoreId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(package => package.CourierUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(package => package.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(package => package.DispatchedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(package => package.ReceivedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(package => package.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(package => package.CourierUser)
            .WithMany()
            .HasForeignKey(package => package.CourierUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(package => package.SourceStore)
            .WithMany()
            .HasForeignKey(package => package.SourceStoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(package => package.DestinationStore)
            .WithMany()
            .HasForeignKey(package => package.DestinationStoreId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(package => package.CourierUser)
            .WithMany()
            .HasForeignKey(package => package.CourierUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(package => package.Items)
            .WithOne()
            .HasForeignKey(item => item.PackageId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(package => package.Events)
            .WithOne()
            .HasForeignKey(packageEvent => packageEvent.PackageId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(package => package.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(package => package.Events).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}