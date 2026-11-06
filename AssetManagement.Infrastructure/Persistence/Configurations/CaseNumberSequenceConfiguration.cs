using AssetManagement.Domain.Entities.CaseNumbers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class CaseNumberSequenceConfiguration : IEntityTypeConfiguration<CaseNumberSequence>
{
    public void Configure(EntityTypeBuilder<CaseNumberSequence> builder)
    {
        builder.ToTable("CaseNumberSequences");
        builder.HasKey(sequence => new
            {
                sequence.Prefix,
                sequence.Year
            });

        builder.Property(sequence => sequence.Prefix).HasMaxLength(10).IsRequired();
        builder.Property(sequence => sequence.Year).IsRequired();
        builder.Property(sequence => sequence.LastNumber).IsRequired();
        builder.Property(sequence => sequence.RowVersion).IsRowVersion().IsConcurrencyToken().IsRequired();
    }
}
