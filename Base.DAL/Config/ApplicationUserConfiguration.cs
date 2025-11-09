using Base.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Config
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            //builder.HasOne<SystemAdminProfile>(e=>e.SystemAdminProfile)
            //    .WithOne(p => p.User)
            //    .HasForeignKey<SystemAdminProfile>(p=>p.UserId);

            //builder.HasOne<UserProfile>(e => e.Profile)
            //    .WithOne(p => p.User)
            //    .HasForeignKey<UserProfile>(p => p.UserId);

            //builder.HasOne<ClincAdminProfile>(e => e.ClincAdminProfile)
            //    .WithOne(p => p.User)
            //    .HasForeignKey<ClincAdminProfile>(p => p.UserId);

            //builder.HasOne<ClincDoctorProfile>(e => e.ClincDoctorProfile)
            //    .WithOne(p => p.User)
            //    .HasForeignKey<ClincDoctorProfile>(p => p.UserId);

            //builder.HasOne<ClincReceptionistProfile>(e => e.ClincReceptionistProfile)
            //    .WithOne(p => p.User)
            //    .HasForeignKey<ClincReceptionistProfile>(p => p.UserId);

            builder
        .HasOne(u => u.Profile)
        .WithOne(p => p.User)
        .HasForeignKey<UserProfile>(p => p.UserId)
        .OnDelete(DeleteBehavior.Restrict);

            builder
        .HasOne(u => u.ClincAdminProfile)
        .WithOne(p => p.User)
        .HasForeignKey<ClincAdminProfile>(p => p.UserId)
        .OnDelete(DeleteBehavior.Restrict);

            builder
                 .HasOne(u => u.ClincDoctorProfile)
                 .WithOne(p => p.User)
                 .HasForeignKey<ClincDoctorProfile>(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder
                 .HasOne(u => u.ClincReceptionistProfile)
                 .WithOne(p => p.User)
                 .HasForeignKey<ClincReceptionistProfile>(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);

            builder
                 .HasOne(u => u.SystemAdminProfile)
                 .WithOne(p => p.User)
                 .HasForeignKey<SystemAdminProfile>(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
