using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.ServiceDesk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures persistence for ServiceDesk request creation operations.
/// </summary>
internal sealed class ServiceDeskRequestOutboxConfiguration
    : IEntityTypeConfiguration<ServiceDeskRequestOutbox>
{
    public void Configure(
        EntityTypeBuilder<ServiceDeskRequestOutbox> builder)
    {
        builder.ToTable("ServiceDeskRequestOutbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ServiceCaseId).IsRequired();
        builder.Property(x => x.ServiceCaseEventId).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvchar(max)").IsRequired();
        builder.Property(x => x.ServiceDeskRequestId).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.ProcessingOperationId).IsRequired(false);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.ProcessingStartedAtUtc).IsRequired(false);
        builder.Property(x => x.ProcessedAtUtc).IsRequired(false);
        builder.Property(x => x.Error).HasMaxLength(1000).IsRequired(false);

        builder.HasIndex(x => x.ServiceCaseId).IsUnique()
            .HasDatabaseName("UX_ServiceDeskRequestOutbox_ServiceCaseId");
        builder.HasIndex(x => x.ServiceCaseEventId).IsUnique()
            .HasDatabaseName("UX_ServiceDeskRequestOutbox_ServiceCaseEventId");

        builder.HasIndex(x => new
            {
                x.Status,
                x.CreatedAtUtc
            })
            .HasDatabaseName("IX_ServiceDeskRequestOutbox_Status_CreatedAtUtc");

        builder.HasIndex(x => new
            {
                x.Status,
                x.ProcessingStartedAtUtc
            })
            .HasDatabaseName("IX_ServiceDeskRequestOutbox_Status_ProcessingStartedAtUtc");

        builder.HasIndex(x => x.ServiceDeskRequestId)
            .IsUnique()
            .HasFilter("[ServiceDeskRequestId] IS NOT NULL")
            .HasDatabaseName("UX_ServiceDeskRequestOutbox_ServiceDeskRequestId");

        builder.HasOne<ServiceCase>()
            .WithMany()
            .HasForeignKey(x => x.ServiceCaseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ServiceCaseEvent>()
            .WithMany()
            .HasForeignKey(x => x.ServiceCaseEventId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
