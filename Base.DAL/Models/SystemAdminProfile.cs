using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models
{
    public class SystemAdminProfile : BaseEntity
    {
        //public int Id { get; set; }
        public string? UserId { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }
    }
    /*public class ClincProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public Clinic? Clinc { get; set; }
    }*/
    public class ClincAdminProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public Clinic? Clinc { get; set; }
    }
    public class ClincDoctorProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public Clinic? Clinc { get; set; }
    }
    public class ClincReceptionistProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public Clinic? Clinc { get; set; }
    }
    public class Clinic : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } 

        //public ICollection<ClincProfile> ClincAdmin { get; set; } = new HashSet<ClincProfile>();

        public ICollection<ClincAdminProfile> ClincAdmin { get; set; } = new HashSet<ClincAdminProfile>();
        public ICollection<ClincDoctorProfile> ClincDoctor { get; set; } = new HashSet<ClincDoctorProfile>();
        public ICollection<ClincReceptionistProfile> ClincReceptionis { get; set; } = new HashSet<ClincReceptionistProfile>();
        public string? MedicalSpecialtyId { get; set; }
        public MedicalSpecialty? MedicalSpecialty { get; set; }
    }
    public class MedicalSpecialty : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    public class UserType : BaseEntity
    {
        public string Name { get; set; }
    }

}
