using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    public class EmployeeProfileConfig : BaseEntityConfigurations<EmployeeProfile>
    {
        public override void Configure(EntityTypeBuilder<EmployeeProfile> builder)
        {
            // 1. IMPORTANT: Call Base Configuration
            base.Configure(builder);

            // 2. Configure the 1-to-1 Owner Relationship
            builder.HasKey(p => p.UserId);

            builder.Property(p => p.Salary).HasColumnType("decimal(18,2)");

            builder.HasOne(p => p.User)
                    .WithOne(u => u.EmployeeData)
                    .HasForeignKey<EmployeeProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            // 3. Resolve Ambiguity for Audit Fields
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