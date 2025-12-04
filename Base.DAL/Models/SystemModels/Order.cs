using Base.DAL.Models.BaseModels; // Assuming ApplicationUser & BaseEntity are here
using Base.DAL.Models.SystemModels.Enums;
namespace Base.DAL.Models.SystemModels
{
    public class Order : BaseEntity
    {
        public decimal TotalAmount { get; set; }
        public decimal CommissionAmount { get; set; } // Snapshot of commission value
        public OrderStatus Status { get; set; }
        public DateTime? ApprovedDate { get; set; }

        // Foreign Key to Customer
        public string CustomerId { get; set; }
        public virtual ApplicationUser Customer { get; set; }

        // Foreign Key to Sales Rep (Nullable)
        public string? SalesRepId { get; set; }
        public virtual ApplicationUser SalesRep { get; set; }

        // Collections
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<Invoice> Invoices { get; set; }
    }
}