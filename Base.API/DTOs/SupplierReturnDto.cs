using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Base.API.DTOs;

public class SupplierReturnDto 
{
      [Key]
      public string SupplierId { get; set; }
      [Required]
      [MaxLength(250)]
      public string Name { get; set; }
      [Required]
      [MaxLength(250)]
      public string? ContactInfo { get; set; }
      [MaxLength(250)]
      public string? Address { get; set; }
}