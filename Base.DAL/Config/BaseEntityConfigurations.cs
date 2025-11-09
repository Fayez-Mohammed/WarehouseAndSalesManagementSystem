using Base.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Config
{
    public class BaseEntityConfigurations<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasOne(e => e.CreatedBy)
            .WithMany().HasForeignKey(e => e.CreatedById);
            //.OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.UpdatedBy)
            .WithMany().HasForeignKey(e => e.UpdatedById);
            //.OnDelete(DeleteBehavior.SetNull);
        }
    }

}
