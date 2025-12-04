using Base.DAL.Models.BaseModels; // Assuming ApplicationUser & BaseEntity are here

namespace Base.DAL.Models.SystemModels
{
    public class OrderItem : BaseEntity
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // Price at moment of purchase

        public string OrderId { get; set; }
        public virtual Order Order { get; set; }

        public string ProductId { get; set; }
        public virtual Product Product { get; set; }
    }
}