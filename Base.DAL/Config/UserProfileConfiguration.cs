using Base.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Config
{
    public class UserProfileConfigurations : BaseEntityConfigurations<UserProfile>
    {
        public override void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
    public class SystemAdminProfileConfigurations : BaseEntityConfigurations<SystemAdminProfile>
    {
        public override void Configure(EntityTypeBuilder<SystemAdminProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
    public class ClincAdminProfileConfigurations : BaseEntityConfigurations<ClincAdminProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincAdminProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
    public class ClincReceptionistProfileConfigurations : BaseEntityConfigurations<ClincReceptionistProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincReceptionistProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
}
