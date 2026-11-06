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
            builder.HasIndex(store => store.StoreNumber).IsUnique();
            ConfigureAddress(builder);
        }

        private static void ConfigureAddress(EntityTypeBuilder<Store> builder)
        {
            builder.ComplexProperty(store => store.Address, addressBuilder =>
            {
                addressBuilder.IsRequired();

                addressBuilder.Property(address => address.CountryCode)
                .HasConversion<string>()
                .HasColumnName("CountryCode")
                .HasMaxLength(5)
                .IsRequired();
                addressBuilder.Property(address => address.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(5)
                .IsRequired();
                addressBuilder.Property(address => address.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();
                addressBuilder.Property(address => address.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired();
                addressBuilder.Property(address => address.PublicSpace)
                .HasConversion<string>()
                .HasColumnName("PublicSpace")
                .HasMaxLength(20)
                .IsRequired();
                addressBuilder.Property(address => address.HouseNumber)
                .HasColumnName("HouseNumber")
                .HasMaxLength(10)
                .IsRequired();
            });
        }
    }
}
