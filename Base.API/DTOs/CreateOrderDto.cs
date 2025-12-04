using System.ComponentModel.DataAnnotations;

namespace Base.API.DTOs
{
    public class CreateOrderDto
    {
        // Optional: The Sales Rep ID can be provided if a rep places the order for a customer.
        // If null, it can be assigned later during the confirmation process.
        public string? SalesRepId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> Items { get; set; }
    }

    public class CreateOrderItemDto
    {
        [Required]
        public string ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }
    }
}
