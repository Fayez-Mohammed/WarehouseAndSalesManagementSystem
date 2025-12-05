using Base.API.DTOs;
using FluentValidation;

namespace BaseAPI.Validation.SupplierValidation;

public class SupplierPostValidation : AbstractValidator<SupplierPostDto>
{
    public SupplierPostValidation()
    {
        RuleFor(x=>x.Name)
            .NotEmpty()
            .MaximumLength(250);
        RuleFor(x => x.ContactInfo)
            .MaximumLength(250);
        
        RuleFor(x => x.Address)
            .MaximumLength(250);
    }
}