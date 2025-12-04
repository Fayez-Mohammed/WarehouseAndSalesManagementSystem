namespace Base.DAL.Models.SystemModels.Enums
{
    // =========================================================
    // 1. Enums
    // =========================================================
    public enum OrderStatus
    {
        Pending = 1,        // Draft by Customer
        Confirmed = 2,      // Reviewed by Sales Rep
        Approved = 3,       // Stock Deducted by Store Manager
        Rejected = 4,       // Cancelled
        Completed = 5       // Delivered
    }
}