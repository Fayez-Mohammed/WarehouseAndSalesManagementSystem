using Base.API.DTOs;
using FluentValidation;

namespace BaseAPI.Validation.ProductValidation;

public class ProductDtoValidation : AbstractValidator<ProductDto>
{

  public ProductDtoValidation()
  {
    RuleFor(x => x.Quantity)
      .GreaterThan(0);
    RuleFor(x => x.Price)
      .GreaterThan(0);
    RuleFor(x => x.ProductName)
      .NotEmpty();
    RuleFor(x => x.SKU)
      .NotEmpty();
  }
}