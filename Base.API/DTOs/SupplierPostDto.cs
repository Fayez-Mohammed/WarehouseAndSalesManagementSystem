using System.ComponentModel.DataAnnotations;

namespace Base.API.DTOs;

public class SupplierPostDto
{
    [Required]
    [MaxLength(250)]
    public string Name { get; set; }
    [Required]
    [MaxLength(250)]
    public string? ContactInfo { get; set; }
    [MaxLength(250)]
    public string? Address { get; set; }
}