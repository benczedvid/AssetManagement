using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="ServiceDeskRequestClosureOutbox"/> entity.
/// </summary>
/// <remarks>
/// Each service case can have at most one external
/// ServiceDesk request closure operation.
///
/// Closure operations are processed only after all related
/// ServiceDesk task synchronization operations have completed.
/// </remarks>
internal sealed class ServiceDeskRequestClosureOutboxConfiguration : IEntityTypeConfiguration<ServiceDeskRequestClosureOutbox>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        builder.ToTable("ServiceDeskRequestClosureOutbox");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        builder.HasKey(outbox => outbox.Id);
        builder.Property(outbox => outbox.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        builder.Property(outbox => outbox.ServiceCaseId).IsRequired();
        builder.Property(outbox => outbox.ServiceCaseEventId).IsRequired();
        builder.Property(outbox => outbox.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(outbox => outbox.ProcessingOperationId).IsRequired(false);
        builder.Property(outbox => outbox.CreatedAtUtc).IsRequired();
        builder.Property(outbox => outbox.ProcessingStartedAtUtc).IsRequired(false);
        builder.Property(outbox => outbox.ProcessedAtUtc).IsRequired(false);
        builder.Property(outbox => outbox.Error).HasMaxLength(1000).IsRequired(false);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        // Only one closure operation is allowed per service case.
        builder.HasIndex(outbox => outbox.ServiceCaseId).IsUnique()
            .HasDatabaseName("UX_ServiceDeskRequestClosureOutbox_ServiceCaseId");

        // Each service case event can trigger at most one closure.
        builder.HasIndex(outbox => outbox.ServiceCaseEventId).IsUnique()
            .HasDatabaseName("UX_ServiceDeskRequestClosureOutbox_ServiceCaseEventId");

        // Supports efficient retrieval of pending closure operations.
        builder.HasIndex(outbox => new
            {
                outbox.Status,
                outbox.CreatedAtUtc
            })
            .HasDatabaseName("IX_ServiceDeskRequestClosureOutbox_Status_CreatedAtUtc");

        // Supports detection of stale processing operations.
        builder.HasIndex(outbox => new
            {
                outbox.Status,
                outbox.ProcessingStartedAtUtc
            })
            .HasDatabaseName("IX_ServiceDeskRequestClosureOutbox_Status_ProcessingStartedAtUtc");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<ServiceDeskRequestClosureOutbox> builder)
    {
        builder.HasOne<ServiceCase>()
            .WithMany()
            .HasForeignKey(outbox => outbox.ServiceCaseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ServiceCaseEvent>()
            .WithMany()
            .HasForeignKey(outbox => outbox.ServiceCaseEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}