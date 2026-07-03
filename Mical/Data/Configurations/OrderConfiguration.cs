using Mical.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mical.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(o => o.OrderNumber).IsUnique();

        builder.Property(o => o.UserId).IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<int>();

        builder.Property(o => o.Total).HasPrecision(12, 2);

        builder.Property(o => o.ContactName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.ContactPhone).IsRequired().HasMaxLength(30);
        builder.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(300);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);

        // FK a AspNetUsers (Restrict: no borrar usuarios con pedidos).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order!)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
