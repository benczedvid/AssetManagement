using AssetManagement.Domain.Entities.Vendors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Graph.Models.ExternalConnectors;


namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("Vendors");
            builder.HasKey(vendor => vendor.VendorId);
            builder.Property(vendor => vendor.Name).IsRequired().HasMaxLength(200);
            builder.Property(vendor => vendor.Webpage).IsRequired(false).HasMaxLength(2048);
            ConfigureAddress(builder);
            ConfigureContacts(builder);
        }

        private static void ConfigureAddress(EntityTypeBuilder<Vendor> builder)
        {
            builder.ComplexProperty(vendor => vendor.Address,
                addressBuilder =>
                {
                    addressBuilder.Property(address => address.CountryCode)
                    .HasConversion<string>()
                    .HasColumnName("CountryCode")
                    .HasMaxLength(3)
                    .IsRequired();

                    addressBuilder.Property(address => address.PostalCode)
                    .HasColumnName("PostalCode")
                    .HasMaxLength(10)
                    .IsRequired();

                    addressBuilder.Property(address => address.City)
                    .HasColumnName("City")
                    .HasMaxLength(100)
                    .IsRequired();

                    addressBuilder.Property(address => address.Street)
                    .HasColumnName("Street")
                    .HasMaxLength(100)
                    .IsRequired();

                    addressBuilder.Property(address => address.PublicSpace)
                    .HasConversion<string>()
                    .HasColumnName("PublicSpace")
                    .HasMaxLength(50)
                    .IsRequired();

                    addressBuilder.Property(address => address.HouseNumber)
                    .HasColumnName("HouseNumber")
                    .HasMaxLength(10)
                    .IsRequired();
                });
        }

        private static void ConfigureContacts(EntityTypeBuilder<Vendor> builder)
        {
            builder.HasMany(vendor => vendor.Contacts)
                .WithOne()
                .HasForeignKey(vendor => vendor.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(vendor => vendor.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
