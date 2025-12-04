using System;
using System.Collections.Generic;
using Base.DAL.Models.BaseModels; // Assuming ApplicationUser & BaseEntity are here

namespace Base.DAL.Models.SystemModels
{

    // =========================================================
    // 2. Domain Entities
    // =========================================================

    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string SKU { get; set; }
        public string? Description { get; set; }

        public decimal BuyPrice { get; set; } // Cost
        public decimal SellPrice { get; set; } // Price for Customer
        public int CurrentStockQuantity { get; set; }

        public virtual ICollection<StockTransaction> StockTransactions { get; set; }
    }
}