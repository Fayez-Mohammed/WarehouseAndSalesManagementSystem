using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    public class UserProfileConfigurations : BaseEntityConfigurations<UserProfile>
    {
        public override void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // 1. Call Base Config (Sets up ID, CreatedDate, etc.)
            base.Configure(builder);

            // 2. Configure the Main 1-to-1 Relationship (Owner)
            builder.HasKey(p => p.UserId);

            // We link back to the ApplicationUser. 
            // Note: Ensure your UserProfile class has a property named 'ApplicationUser'. 
            // If it is named 'User', change .ApplicationUser below to .User
            builder.HasOne(p => p.User)
                   .WithOne(u => u.Profile)
                   .HasForeignKey<UserProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 3. CRITICAL FIX: Resolve Audit Conflicts (CreatedBy/UpdatedBy)
            // Explicitly tell EF these are separate from the User relationship above
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