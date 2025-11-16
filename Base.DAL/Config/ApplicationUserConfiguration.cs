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
    /* public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
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
     }*/

    // Base configuration for BaseEntity
    //public class BaseEntityConfigurations<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
    //{
    //    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    //    {
    //        builder.HasKey(e => e.Id);

    //        builder.HasOne(e => e.CreatedBy)
    //               .WithMany()
    //               .HasForeignKey(e => e.CreatedById)
    //               .OnDelete(DeleteBehavior.SetNull);

    //        builder.HasOne(e => e.UpdatedBy)
    //               .WithMany()
    //               .HasForeignKey(e => e.UpdatedById)
    //               .OnDelete(DeleteBehavior.SetNull);

    //        builder.Property(e => e.DateOfCreattion)
    //               .HasDefaultValueSql("GETDATE()");

    //        builder.Property(e => e.DateOfUpdate)
    //               .HasDefaultValueSql("GETDATE()");
    //    }
    //}

    // ApplicationUser configuration
    // BaseEntityConfigurations already exists
    // ApplicationUser Configuration
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            builder.Property(u => u.UserType).IsRequired().HasMaxLength(50);

            builder.HasOne(u => u.Profile)
                   .WithOne(p => p.User)
                   .HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.SystemAdminProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<SystemAdminProfile>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.ClincAdminProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincAdminProfile>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.ClincDoctorProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincDoctorProfile>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.ClincReceptionistProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincReceptionistProfile>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

        }
    }

    public class RefreshTokenConfiguration : BaseEntityConfigurations<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);

            // TokenHash is unique
            builder.Property(r => r.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(r => r.TokenHash)
                .IsUnique();

            // Required properties
            builder.Property(r => r.CreatedByIp)
                .HasMaxLength(45) // IPv4 + IPv6
                .IsRequired();

            builder.Property(r => r.CreatedByUserAgent)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(r => r.RevokedByIp)
                .HasMaxLength(45);

            builder.Property(r => r.ReplacedByTokenHash)
                .HasMaxLength(64);

            builder.Property(r => r.ReasonRevoked)
                .HasMaxLength(256);

            // علاقة (User 1 ---- * RefreshTokens)
            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // RowVersion (Concurrency Token)
            builder.Property(r => r.RowVersion)
                .IsRowVersion();
        }
    }
    // Clinic Configuration
    public class ClinicConfiguration : BaseEntityConfigurations<Clinic>
    {
        public override void Configure(EntityTypeBuilder<Clinic> builder)
        {
            base.Configure(builder);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Email).HasMaxLength(100);
            builder.Property(c => c.Status).HasMaxLength(50);

            builder.HasMany(c => c.ClincAdmin)
                   .WithOne(a => a.Clinc)
                   .HasForeignKey(a => a.ClincId);

            builder.HasMany(c => c.ClincDoctor)
                   .WithOne(d => d.Clinc)
                   .HasForeignKey(d => d.ClincId);

            builder.HasMany(c => c.ClincReceptionis)
                   .WithOne(r => r.Clinc)
                   .HasForeignKey(r => r.ClincId);

            builder.HasMany(c => c.ClinicSchedules)
                   .WithOne(s => s.Clinic)
                   .HasForeignKey(s => s.ClinicId);
        }
    }

    // ClincDoctorProfile Configuration
    public class ClincDoctorProfileConfiguration : BaseEntityConfigurations<ClincDoctorProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincDoctorProfile> builder)
        {
            base.Configure(builder);

            builder.HasMany(d => d.Schedules)
                   .WithOne(s => s.Doctor)
                   .HasForeignKey(s => s.DoctorId);
        }
    }

    // ClinicSchedule Configuration
    public class ClinicScheduleConfiguration : BaseEntityConfigurations<ClinicSchedule>
    {
        public override void Configure(EntityTypeBuilder<ClinicSchedule> builder)
        {
            base.Configure(builder);

            builder.HasOne(s => s.Clinic)
                   .WithMany(c => c.ClinicSchedules)
                   .HasForeignKey(s => s.ClinicId);

            builder.HasOne(s => s.Doctor)
                   .WithMany(d => d.Schedules)
                   .HasForeignKey(s => s.DoctorId);

            builder.HasMany(s => s.AppointmentSlots)
                   .WithOne(a => a.DoctorSchedule)
                   .HasForeignKey(a => a.ClinicScheduleId);
        }
    }

    // AppointmentSlot Configuration
    public class AppointmentSlotConfiguration : BaseEntityConfigurations<AppointmentSlot>
    {
        public override void Configure(EntityTypeBuilder<AppointmentSlot> builder)
        {
            base.Configure(builder);

            builder.HasOne(a => a.DoctorSchedule)
                   .WithMany(s => s.AppointmentSlots)
                   .HasForeignKey(a => a.ClinicScheduleId);

            builder.HasOne(a => a.Appointment)
                   .WithOne(ap => ap.Slot)
                   .HasForeignKey<Appointment>(ap => ap.SlotId);
        }
    }

    // Appointment Configuration
    public class AppointmentConfiguration : BaseEntityConfigurations<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);

            builder.HasOne(a => a.Patient)
                   .WithMany()
                   .HasForeignKey(a => a.PatientId);
        }
    }

    // UserProfile Configuration
    public class UserProfileConfiguration : BaseEntityConfigurations<UserProfile>
    {
        public override void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            base.Configure(builder);

            builder.Property(p => p.FullName).IsRequired().HasMaxLength(150);
        }
    }

    // MedicalSpecialty Configuration
    public class MedicalSpecialtyConfiguration : BaseEntityConfigurations<MedicalSpecialty>
    {
        public override void Configure(EntityTypeBuilder<MedicalSpecialty> builder)
        {
            base.Configure(builder);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
        }
    }

    // UserType Configuration
    public class UserTypeConfiguration : BaseEntityConfigurations<UserType>
    {
        public override void Configure(EntityTypeBuilder<UserType> builder)
        {
            base.Configure(builder);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(50);
        }
    }


}
