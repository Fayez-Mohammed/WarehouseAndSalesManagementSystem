using Base.DAL.Config.BaseConfig; // Ensure you import your Base Config namespace
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // =========================================================
    // Product & Supplier Configs
    // =========================================================

    public class ProductConfiguration : BaseEntityConfigurations<Product>
    {
        public override void Configure(EntityTypeBuilder<Product> builder)
        {
            base.Configure(builder); // Call Base for ID and defaults

            builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
            builder.Property(p => p.SKU).HasMaxLength(50);

            builder.Property(p => p.BuyPrice).HasColumnType("decimal(18,2)");
            builder.Property(p => p.SellPrice).HasColumnType("decimal(18,2)");

            // Fix Audit (Standard practice now for all BaseEntities)
            builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}