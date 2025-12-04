using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Base.DAL.Models.SystemModels;

namespace Base.DAL.Config.SystemConfig
{
    public class CustomerProfileConfig : IEntityTypeConfiguration<CustomerProfile>
    {
        public void Configure(EntityTypeBuilder<CustomerProfile> builder)
        {
            builder.HasKey(p => p.UserId);
            builder.Property(p => p.CreditLimit).HasColumnType("decimal(18,2)");

            // 1. The Owner Relationship (1-to-1)
            builder.HasOne(p => p.User)
                   .WithOne(u => u.CustomerData)
                   .HasForeignKey<CustomerProfile>(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // 2. The Audit Relationships (Inherited from BaseEntity)
            // <<< FIX HERE: Explicitly define these relationships >>>

            builder.HasOne(e => e.CreatedBy)
                   .WithMany() // No list in User class, so it is empty
                   .HasForeignKey(e => e.CreatedById)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent Cycles

            builder.HasOne(e => e.UpdatedBy)
                   .WithMany()
                   .HasForeignKey(e => e.UpdatedById)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}