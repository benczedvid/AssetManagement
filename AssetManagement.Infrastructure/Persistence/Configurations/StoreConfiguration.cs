using AssetManagement.Domain.Entities.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace AssetManagement.Infrastructure.Persistence.Configurations
{
    public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
    {
        public void Configure(EntityTypeBuilder<Store> builder)
        {
            builder.ToTable("Stores");
            builder.HasKey(store  => store.Id);
            builder.Property(store => store.Id)
                .ValueGeneratedNever();
            builder.Property(store => store.StoreNumber)
                .IsRequired()
                .HasMaxLength(10);
            builder.Property(store => store.Name)
                .IsRequired()
                .HasMaxLength(200);
            ConfigureAddress(builder);
        }

        private static void ConfigureAddress(EntityTypeBuilder<Store> builder)
        {
            builder.ComplexProperty(store => store.Address, addressBuilder =>
            {
                addressBuilder.IsRequired();

                addressBuilder.Property(address => address.CountryCode)
                .HasConversion<string>()
                .HasColumnName("COUNTRYCODE")
                .HasMaxLength(5)
                .IsRequired();
                addressBuilder.Property(address => address.PostalCode)
                .HasColumnName("POSTAL_CODE")
                .HasMaxLength(5)
                .IsRequired();
                addressBuilder.Property(address => address.City)
                .HasColumnName("CITY")
                .HasMaxLength(100)
                .IsRequired();
                addressBuilder.Property(address => address.Street)
                .HasColumnName("STREET")
                .HasMaxLength(200)
                .IsRequired();
                addressBuilder.Property(address => address.PublicSpace)
                .HasConversion<string>()
                .HasColumnName("PUBLIC_SPACE")
                .HasMaxLength(20)
                .IsRequired();
                addressBuilder.Property(address => address.HouseNumber)
                .HasColumnName("HOUSE_NUMBER")
                .HasMaxLength(10)
                .IsRequired();
            });
        }
    }
}
