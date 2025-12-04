using Base.DAL.Models.BaseModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace Base.DAL.Models.SystemModels
{
    public class SalesRepProfile : BaseEntity
    {
        // Foreign Key to the Login User
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Specific Sales Rep Data
        public decimal CommissionRate { get; set; } = 0.10m; // Default 10%
        public string? Region { get; set; } // e.g., "Cairo Branch"
        public decimal TotalSalesYTD { get; set; } // Year-to-Date Sales Cache
    }
}