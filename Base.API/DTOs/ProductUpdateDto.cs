namespace Base.API.DTOs;

public class ProductUpdateDto
{
    public string? ProductId { get; set; }
    
    public string? ProductName { get; set; }
    
    public decimal SellPrice { get; set; }
    
    public int Quantity { get; set; }
    
    public string? SKU  { get; set; }
    
    public string? Description { get; set; }
    
    public decimal BuyPrice { get; set; }
}