using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string UserType { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ImagePath { get; set; }
       // public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();

        public virtual UserProfile? Profile { get; set; }
        public virtual SystemAdminProfile? SystemAdminProfile { get; set; }
        public virtual ClincAdminProfile? ClincAdminProfile { get; set; }
        public virtual ClincDoctorProfile? ClincDoctorProfile { get; set; }
        public virtual ClincReceptionistProfile? ClincReceptionistProfile { get; set; }

    }
}
