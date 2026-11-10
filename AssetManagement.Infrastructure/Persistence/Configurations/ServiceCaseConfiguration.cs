using AssetManagement.Domain.Entities.Assets;
using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Stores;
using AssetManagement.Domain.Entities.Users;
using AssetManagement.Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="ServiceCase"/> entity.
/// </summary>
internal sealed class ServiceCaseConfiguration
    : IEntityTypeConfiguration<ServiceCase>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceCase> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.ToTable("ServiceCases");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.HasKey(serviceCase => serviceCase.Id);
        builder.Property(serviceCase => serviceCase.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.Property(serviceCase => serviceCase.CaseNumber).HasMaxLength(50).IsRequired();
        builder.Property(serviceCase => serviceCase.AssetId).IsRequired();
        builder.Property(serviceCase => serviceCase.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(serviceCase => serviceCase.IssueType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(serviceCase => serviceCase.OtherIssueDescription).HasMaxLength(250).IsRequired(false);
        builder.Property(serviceCase => serviceCase.Description).HasMaxLength(2000).IsRequired();
        builder.Property(serviceCase => serviceCase.OriginStoreId).IsRequired(false);
        builder.Property(serviceCase => serviceCase.VendorId).IsRequired(false);
        builder.Property(serviceCase => serviceCase.CreatedByUserId).IsRequired();
        builder.Property(serviceCase => serviceCase.CreatedByUserName).HasMaxLength(250).IsRequired();
        builder.Property(serviceCase => serviceCase.CreatedAtUtc).IsRequired();
        builder.Property(serviceCase => serviceCase.ClosedByUserId).IsRequired(false);
        builder.Property(serviceCase => serviceCase.ClosedAtUtc).IsRequired(false);
        builder.Ignore(serviceCase => serviceCase.IsActive);
        builder.Property(serviceCase => serviceCase.ServiceDeskRequestId).HasMaxLength(100);
        builder.Property(serviceCase => serviceCase.ServiceDeskSyncStatus).HasConversion<string>().HasMaxLength(50).IsRequired().HasDefaultValue(ServiceDeskSyncStatus.NotStarted);;
        builder.Property(serviceCase => serviceCase.ServiceDeskOperationId);
        builder.Property(serviceCase => serviceCase.ServiceDeskSyncStartedAtUtc);
        builder.Property(serviceCase => serviceCase.ServiceDeskSyncCompletedAtUtc);
        builder.Property(serviceCase => serviceCase.ServiceDeskSyncError).HasMaxLength(1000);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.HasIndex(serviceCase => serviceCase.CaseNumber).IsUnique()
            .HasDatabaseName("UX_ServiceCases_CaseNumber");

        builder.HasIndex(serviceCase => serviceCase.AssetId)
            .HasDatabaseName("IX_ServiceCases_AssetId");

        builder.HasIndex(serviceCase => serviceCase.Status)
            .HasDatabaseName("IX_ServiceCases_Status");

        builder.HasIndex(serviceCase => serviceCase.OriginStoreId)
            .HasDatabaseName("IX_ServiceCases_OriginStoreId");

        builder.HasIndex(serviceCase => serviceCase.VendorId)
            .HasDatabaseName("IX_ServiceCases_VendorId");

        builder.HasIndex(serviceCase => serviceCase.CreatedByUserId)
            .HasDatabaseName("IX_ServiceCases_CreatedByUserId");

        builder.HasIndex(serviceCase => serviceCase.ClosedByUserId)
            .HasDatabaseName("IX_ServiceCases_ClosedByUserId");

        builder.HasIndex(
                serviceCase => new
                {
                    serviceCase.AssetId,
                    serviceCase.Status
                })
            .HasDatabaseName("IX_ServiceCases_AssetId_Status");
        
        builder.HasIndex(serviceCase => serviceCase.ServiceDeskRequestId)
            .IsUnique()
            .HasFilter("[ServiceDeskRequestId] IS NOT NULL");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(serviceCase => serviceCase.AssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(serviceCase => serviceCase.OriginStoreId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(serviceCase => serviceCase.VendorId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(serviceCase => serviceCase.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(serviceCase => serviceCase.ClosedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}