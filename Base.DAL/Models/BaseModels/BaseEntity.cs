//using Microsoft.AspNetCore.Identity;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Base.DAL.Models.BaseModels
//{
//    public class BaseEntity
//    {
//        public string Id { get; set; }
//        public string? CreatedById { get; set; }
//        //public virtual ApplicationUser? CreatedBy { get; set; }
//        public DateTime DateOfCreattion { get; set; }

//        public string? UpdatedById { get; set; }
//        //public virtual ApplicationUser? UpdatedBy { get; set; }
//        public DateTime DateOfUpdate { get; set; }
//        public BaseEntity()
//        {
//            Id = Guid.NewGuid().ToString();
//        }
//    }
//}

using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace Base.DAL.Models.BaseModels
{
    public class BaseEntity
    {
        [Key]
        public string Id { get; set; }

        // =======================================================
        // 1. Creation Audit
        // =======================================================
        public string? CreatedById { get; set; }

        // I recommend uncommenting this. It allows you to easily display 
        // "Created By: Ahmed" in your UI without complex joins.
        public virtual ApplicationUser? CreatedBy { get; set; }

        // Fixed typo: DateOfCreattion -> DateOfCreation
        public DateTime DateOfCreation { get; set; } = DateTime.UtcNow;

        // =======================================================
        // 2. Update Audit
        // =======================================================
        public string? UpdatedById { get; set; }
        public virtual ApplicationUser? UpdatedBy { get; set; }

        // Changed to Nullable (DateTime?)
        // If it is null, we know the record was NEVER updated.
        public DateTime? DateOfUpdate { get; set; }

        // =======================================================
        // 3. Soft Delete (The most important addition)
        // =======================================================
        public bool IsDeleted { get; set; } = false;
        public DateTime? DateOfDeletion { get; set; }
        public string? DeletedById { get; set; }

        public BaseEntity()
        {
            Id = Guid.NewGuid().ToString();
            DateOfCreation = DateTime.UtcNow;
        }
    }
}