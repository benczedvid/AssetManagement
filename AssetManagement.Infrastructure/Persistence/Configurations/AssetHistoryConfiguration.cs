using AssetManagement.Domain.Entities.AssetHistories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    public sealed class AssetHistoryConfiguration : IEntityTypeConfiguration<AssetHistory>
    {
        public void Configure(EntityTypeBuilder<AssetHistory> builder)
        {
            builder.ToTable("AssetHistories");

            builder.HasKey(history => history.Id);

            builder.Property(history => history.Id)
                .ValueGeneratedNever();

            builder.Property(history => history.AssetId)
                .IsRequired();

            builder.Property(history => history.Action)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(history => history.PropertyName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(history => history.OldValue)
                .HasMaxLength(2000);

            builder.Property(history => history.NewValue)
                .HasMaxLength(2000);

            builder.Property(history => history.ChangedByUserId)
                .IsRequired();

            builder.Property(history => history.ChangedByUserName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(history => history.ChangedAtUtc)
                .IsRequired();

            builder.HasIndex(history => history.AssetId);

            builder.HasIndex(history => new
            {
                history.AssetId,
                history.ChangedAtUtc
            });
        }
    }
}