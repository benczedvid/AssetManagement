using AssetManagement.Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.ToTable("Contacts");
            builder.HasKey(contact => contact.ContactId);
            builder.Property(contact => contact.ContactId).ValueGeneratedNever();
            builder.Property(contact => contact.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(contact => contact.LastName).HasMaxLength(100).IsRequired();
            builder.Property(contact => contact.JobTitle).HasMaxLength(100).IsRequired();
            builder.Property(contact => contact.EmailAddress).HasMaxLength(200).IsRequired();
            builder.Property(contact => contact.PhoneNumber).HasMaxLength(30).IsRequired();

            builder.HasIndex(contact => new
            {
                contact.VendorId,
                contact.PhoneNumber
            }).IsUnique();
        }
    }
}
