using System.ComponentModel.DataAnnotations;

namespace Base.API.DTOs;

public class ProductCreateDto
{ 
    [Required]
    [MaxLength(100)]
    public string? ProductName { get; set; }
    [Required]
    [Range(0.1, double.MaxValue)]
    public decimal SellPrice { get; set; }
    [Range(0.1, double.MaxValue)]
    public decimal BuyPrice { get; set; }
    [Required]
    [Range(0.1, int.MaxValue)]
    public int Quantity { get; set; }
    [MaxLength(100)]
    public string? SKU  { get; set; }
    public string? Description { get; set; }
    
}