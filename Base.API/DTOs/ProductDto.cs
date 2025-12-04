namespace Base.API.DTOs;

public class ProductDto
{
    public string? ProductId { get; set; }
    
    public string? ProductName { get; set; }
    
    public decimal Price { get; set; }
    
    public int Quantity { get; set; }
    
    public string? SKU  { get; set; }
    
    public string? Description { get; set; }
    
}