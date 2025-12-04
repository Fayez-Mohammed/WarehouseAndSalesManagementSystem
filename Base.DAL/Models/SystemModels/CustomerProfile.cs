using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class CustomerProfile : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        // Specific Customer Data
        public string ShippingAddress { get; set; }
        public string City { get; set; }
        public decimal CreditLimit { get; set; } = 0; // If you allow credit
        public int LoyaltyPoints { get; set; }
    }
}