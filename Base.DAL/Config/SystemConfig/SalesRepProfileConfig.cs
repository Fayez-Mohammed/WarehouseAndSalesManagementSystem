using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    public class SalesRepProfileConfig : BaseEntityConfigurations<SalesRepProfile>
    {
        public override void Configure(EntityTypeBuilder<SalesRepProfile> builder)
        {
            // 1. IMPORTANT: Call the Base Configuration first!
            base.Configure(builder);

            // 2. Configure the 1-to-1 Owner Relationship
            // Set UserId as both PK and FK
            builder.HasKey(p => p.UserId);

            builder.Property(p => p.CommissionRate).HasColumnType("decimal(18,2)");
            builder.Property(p => p.TotalSalesYTD).HasColumnType("decimal(18,2)");

            builder.HasOne(p => p.User)
                   .WithOne(u => u.SalesRepData)
                   .HasForeignKey<SalesRepProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade); // If User deleted, Profile deleted

            // 3. Resolve Ambiguity for Audit Fields (CreatedBy/UpdatedBy)
            // We explicitly tell EF that these use the ID fields, distinct from the User above
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