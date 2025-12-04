using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels.Enums; // Assuming ApplicationUser & BaseEntity are here

namespace Base.DAL.Models.SystemModels
{
    public class Invoice : BaseEntity
    {
        public InvoiceType Type { get; set; }

        // The person receiving the invoice (Customer OR SalesRep)
        public string RecipientName { get; set; }
        public decimal Amount { get; set; }
        public DateTime GeneratedDate { get; set; }

        public string OrderId { get; set; }
        public virtual Order Order { get; set; }
    }
}