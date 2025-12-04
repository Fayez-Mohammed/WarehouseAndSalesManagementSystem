using Base.DAL.Models.BaseModels;
using System;

namespace Base.DAL.Models.SystemModels
{
    public class EmployeeProfile : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public string JobTitle { get; set; } // "Store Manager", "Accountant"
        public string Department { get; set; }
        public DateTime HireDate { get; set; }
        public decimal Salary { get; set; } // Confidential data
    }
}