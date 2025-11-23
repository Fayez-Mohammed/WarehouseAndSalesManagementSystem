using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // School Configuration
    public class SchoolConfiguration : BaseEntityConfigurations<School>
    {
        public override void Configure(EntityTypeBuilder<School> builder)
        {
            base.Configure(builder);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Email).HasMaxLength(100);
            builder.Property(c => c.Status).HasMaxLength(50);

            builder.HasMany(c => c.SchoolAdmin)
                   .WithOne(a => a.School)
                   .HasForeignKey(a => a.SchoolId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }


}
