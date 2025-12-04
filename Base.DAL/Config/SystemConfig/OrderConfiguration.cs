using Base.DAL.Config.BaseConfig; // Ensure you import your Base Config namespace
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // =========================================================
    // Order & Item Configs (THE SOURCE OF YOUR ERROR)
    // =========================================================

    public class OrderConfiguration : BaseEntityConfigurations<Order>
    {
        public override void Configure(EntityTypeBuilder<Order> builder)
        {
            base.Configure(builder);

            builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            builder.Property(o => o.CommissionAmount).HasColumnType("decimal(18,2)");

            // Relationships - Prevent Deletion of Users if they have Orders
            builder.HasOne(o => o.Customer)
                   .WithMany(u => u.OrdersPlaced)
                   .HasForeignKey(o => o.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.SalesRep)
                   .WithMany(u => u.OrdersManaged)
                   .HasForeignKey(o => o.SalesRepId)
                   .OnDelete(DeleteBehavior.Restrict);

            // <<< CRITICAL FIX: Resolve "CreatedBy" Ambiguity >>>
            // EF Core needs to know these are separate from Customer/SalesRep
            builder.HasOne(x => x.CreatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UpdatedBy)
                   .WithMany()
                   .HasForeignKey(x => x.UpdatedById)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}