using AssetManagement.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

public sealed class EmployeeConfiguration
    : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(employee => employee.EmployeeNumber);

        builder.Property(employee => employee.EmployeeNumber)
            .IsRequired()
            .HasMaxLength(10)
            .ValueGeneratedNever();

        builder.Property(employee => employee.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(employee => employee.EmploymentStartDate)
            .IsRequired();
        builder.Property(employee => employee.EmploymentEndDate)
            .IsRequired(false);

        builder.Property(employee => employee.StoreNumber)
            .HasMaxLength(20);

        builder.Property(employee => employee.OrganizationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(employee => employee.PositionCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(employee => employee.Position)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(employee => employee.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(employee => employee.Email)
            .HasMaxLength(320);

        builder.HasIndex(employee => employee.StoreNumber);

        builder.HasIndex(employee => employee.FullName);

        builder.HasIndex(employee => employee.Email);
    }
}