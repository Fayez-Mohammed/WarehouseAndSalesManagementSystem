using Base.DAL.Config.BaseConfig; // Ensure you import your Base Config namespace
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // =========================================================
    // Transaction & Invoice Configs
    // =========================================================

    public class StockTransactionConfiguration : BaseEntityConfigurations<StockTransaction>
    {
        public override void Configure(EntityTypeBuilder<StockTransaction> builder)
        {
            base.Configure(builder);

            builder.HasOne(t => t.Product)
                   .WithMany(p => p.StockTransactions)
                   .HasForeignKey(t => t.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.StoreManager)
                   .WithMany(u => u.TransactionsAuthorized)
                   .HasForeignKey(t => t.StoreManagerId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(t => t.Supplier)
                   .WithMany(s => s.SupplyTransactions)
                   .HasForeignKey(t => t.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);

            // <<< CRITICAL FIX: Resolve "CreatedBy" Ambiguity (StoreManager vs CreatedBy) >>>
            builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.UpdatedBy).WithMany().HasForeignKey(x => x.UpdatedById).OnDelete(DeleteBehavior.Restrict);
        }
    }
}