using Base.DAL.Models.SystemModels;
using Base.Shared.DTOs;
using Base.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.BaseModels
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        //public string UserType { get; set; }
        public UserTypes Type { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ImagePath { get; set; }

        // Existing Profiles
        public virtual UserProfile? Profile { get; set; }
        public virtual SystemAdminProfile? SystemAdminProfile { get; set; }

        // =========================================================
        // NEW: Role-Specific Profiles (Linked 1-to-1)
        // =========================================================
        public virtual SalesRepProfile? SalesRepData { get; set; }
        public virtual CustomerProfile? CustomerData { get; set; }
        public virtual EmployeeProfile? EmployeeData { get; set; }

        // =========================================================
        // Warehouse Relationships
        // =========================================================

        // 1. Orders placed by this user (Customer role)
        // Initialized with new HashSet to prevent NullReferenceException
        public virtual ICollection<Order> OrdersPlaced { get; set; } = new HashSet<Order>();

        // 2. Orders this user is managing (as a Sales Rep)
        public virtual ICollection<Order> OrdersManaged { get; set; } = new HashSet<Order>();

        // 3. Stock transactions this user approved (as a Store Manager)
        public virtual ICollection<StockTransaction> TransactionsAuthorized { get; set; } = new HashSet<StockTransaction>();
    }
}








//using Base.DAL.Models.SystemModels;
//using Base.Shared.DTOs;
//using Base.Shared.Enums;
//using Microsoft.AspNetCore.Identity;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Base.DAL.Models.BaseModels
//{
//    public class ApplicationUser : IdentityUser
//    {
//        public string FullName { get; set; }
//        //public string UserType { get; set; }
//        public UserTypes Type { get; set; }
//        public bool IsActive { get; set; } = true;
//        public string? ImagePath { get; set; }
//        public virtual UserProfile? Profile { get; set; }
//        public virtual SystemAdminProfile? SystemAdminProfile { get; set; }
//        /////////
//        ///
//        public virtual ICollection<Order> OrdersPlaced { get; set; }

//        // 2. Orders this user is managing (as a Sales Rep)
//        public virtual ICollection<Order> OrdersManaged { get; set; }

//        // 3. Stock transactions this user approved (as a Store Manager)
//        public virtual ICollection<StockTransaction> TransactionsAuthorized { get; set; }

//    }
//}
