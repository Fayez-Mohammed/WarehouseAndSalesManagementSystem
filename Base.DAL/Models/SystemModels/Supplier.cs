using Base.DAL.Models.BaseModels; // Assuming ApplicationUser & BaseEntity are here

namespace Base.DAL.Models.SystemModels
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; }
        public string? ContactInfo { get; set; }
        public string? Address { get; set; }

        public virtual ICollection<StockTransaction> SupplyTransactions { get; set; }
    }
}