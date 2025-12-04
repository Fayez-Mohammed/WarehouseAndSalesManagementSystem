using Base.API.DTOs;
using FluentValidation;

namespace BaseAPI.Validation.ProductValidation;

public class ProductUpdateDtoValidation : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateDtoValidation()
    {
        RuleFor(x => x.SellPrice)
            .GreaterThan(0);
        
        RuleFor(x => x.Quantity)
            .GreaterThan(0);
        RuleFor(x => x.BuyPrice)
            .GreaterThan(0);
        RuleFor(x => x.ProductName)
            .NotEmpty();
        RuleFor(x => x.SKU)
            .NotEmpty();
    }
}