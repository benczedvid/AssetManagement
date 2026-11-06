using AssetManagement.Domain.Entities.ServiceCases;
using AssetManagement.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the database mapping for the
/// <see cref="ServiceCaseEvent"/> entity.
/// </summary>
internal sealed class ServiceCaseEventConfiguration : IEntityTypeConfiguration<ServiceCaseEvent>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        ConfigureTable(builder);
        ConfigurePrimaryKey(builder);
        ConfigureProperties(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    private static void ConfigureTable(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        builder.ToTable("ServiceCaseEvents");
    }

    private static void ConfigurePrimaryKey(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        builder.HasKey(serviceCaseEvent => serviceCaseEvent.Id);
        builder.Property(serviceCaseEvent => serviceCaseEvent.Id).ValueGeneratedNever();
    }

    private static void ConfigureProperties(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        builder.Property(serviceCaseEvent => serviceCaseEvent.ServiceCaseId).IsRequired();

        builder.Property(serviceCaseEvent => serviceCaseEvent.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(serviceCaseEvent => serviceCaseEvent.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired(false);

        builder.Property(serviceCaseEvent => serviceCaseEvent.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(serviceCaseEvent => serviceCaseEvent.Comment)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(serviceCaseEvent => serviceCaseEvent.PerformedByUserId)
            .IsRequired();

        builder.Property(serviceCaseEvent => serviceCaseEvent.PerformedByUserName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(serviceCaseEvent => serviceCaseEvent.OccurredAtUtc)
            .IsRequired();
    }

    private static void ConfigureIndexes(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        builder.HasIndex(serviceCaseEvent => serviceCaseEvent.ServiceCaseId)
            .HasDatabaseName("IX_ServiceCaseEvents_ServiceCaseId");

        builder.HasIndex(serviceCaseEvent => serviceCaseEvent.Type)
            .HasDatabaseName("IX_ServiceCaseEvents_Type");

        builder.HasIndex(serviceCaseEvent => serviceCaseEvent.PerformedByUserId)
            .HasDatabaseName("IX_ServiceCaseEvents_PerformedByUserId");

        builder.HasIndex(serviceCaseEvent => new
                {
                    serviceCaseEvent.ServiceCaseId,
                    serviceCaseEvent.OccurredAtUtc
                })
            .HasDatabaseName("IX_ServiceCaseEvents_ServiceCaseId_OccurredAtUtc");
    }

    private static void ConfigureRelationships(EntityTypeBuilder<ServiceCaseEvent> builder)
    {
        builder.HasOne<ServiceCase>()
            .WithMany()
            .HasForeignKey(serviceCaseEvent => serviceCaseEvent.ServiceCaseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(serviceCaseEvent => serviceCaseEvent.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}