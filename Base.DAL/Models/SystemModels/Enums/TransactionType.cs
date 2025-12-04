namespace Base.DAL.Models.SystemModels.Enums
{
    public enum TransactionType
    {
        StockIn = 1,        // Purchase from Supplier
        StockOut = 2,       // Sale to Customer
        Return = 3          // Returned Item
    }
}