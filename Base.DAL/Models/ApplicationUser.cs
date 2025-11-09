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
        public UserProfile? Profile { get; set; }
        public SystemAdminProfile? SystemAdminProfile { get; set; }
        public ClincAdminProfile? ClincAdminProfile { get; set; }
        public ClincDoctorProfile? ClincDoctorProfile { get; set; }
        public ClincReceptionistProfile? ClincReceptionistProfile { get; set; }

    }
}
