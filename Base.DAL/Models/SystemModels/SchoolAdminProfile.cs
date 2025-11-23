using Base.DAL.Models.BaseModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace Base.DAL.Models.SystemModels
{
    public class SchoolAdminProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? SchoolId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(SchoolId))]
        public virtual School? School { get; set; }
    }
}
