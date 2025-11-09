using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models
{
    public class UserProfile:BaseEntity
    {
        //public int Id { get; set; }
        public string? UserId { get; set; } // FK إلى ApplicationUser
        public string FullName { get; set; }
        public string? PhoneNumber { get; set; }

        // Navigation property
        public ApplicationUser? User { get; set; }
    }
}
