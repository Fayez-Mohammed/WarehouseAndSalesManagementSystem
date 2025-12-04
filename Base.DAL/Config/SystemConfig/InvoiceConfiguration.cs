using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{

    public class InvoiceConfiguration : BaseEntityConfigurations<Invoice>
    {
        public override void Configure(EntityTypeBuilder<Invoice> builder)
        {
            base.Configure(builder);
            builder.Property(i => i.Amount).HasColumnType("decimal(18,2)");

            builder.HasOne(i => i.Order)
                   .WithMany(o => o.Invoices)
                   .HasForeignKey(i => i.OrderId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}