using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the persistence model for ServiceDesk request note
/// synchronization outbox entries.
/// </summary>
public sealed class ServiceDeskNoteOutboxConfiguration : IEntityTypeConfiguration<ServiceDeskNoteOutbox>
{
    public void Configure(EntityTypeBuilder<ServiceDeskNoteOutbox> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ServiceDeskNoteOutbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ServiceCaseId).IsRequired();
        builder.Property(x => x.ServiceCaseEventId).IsRequired();
        builder.Property(x => x.ServiceDeskNoteId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ProcessingStartedAtUtc).IsRequired(false);
        builder.Property(x => x.ProcessingOperationId).IsRequired(false);
        builder.Property(x => x.ProcessedAtUtc).IsRequired(false);
        builder.Property(x => x.Error).HasMaxLength(1000).IsRequired(false);

        // Each service case event can produce only one request note.
        builder.HasIndex(x => x.ServiceCaseEventId).IsUnique()
            .HasDatabaseName("UX_ServiceDeskNoteOutbox_ServiceCaseEventId");

        // Prevent storing the same external note identifier twice.
        builder.HasIndex(x => x.ServiceDeskNoteId)
            .IsUnique()
            .HasFilter("[ServiceDeskNoteId] IS NOT NULL")
            .HasDatabaseName("UX_ServiceDeskNoteOutbox_ServiceDeskNoteId");

        // Support lookups by service case.
        builder.HasIndex(x => x.ServiceCaseId)
            .HasDatabaseName("IX_ServiceDeskNoteOutbox_ServiceCaseId");

        // Support pending operation processing.
        builder.HasIndex(x => new
        {
            x.Status,
            x.CreatedAtUtc
        })
        .HasDatabaseName("IX_ServiceDeskNoteOutbox_Status_CreatedAtUtc");

        // Support stale processing reconciliation.
        builder.HasIndex(x => new
        {
            x.Status,
            x.ProcessingStartedAtUtc
        })
        .HasDatabaseName("IX_ServiceDeskNoteOutbox_Status_ProcessingStartedAtUtc");

        // Service case relationship.
        builder.HasOne<ServiceCase>()
            .WithMany()
            .HasForeignKey(x => x.ServiceCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Service case event relationship.
        builder.HasOne<ServiceCaseEvent>()
            .WithMany()
            .HasForeignKey(x => x.ServiceCaseEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}