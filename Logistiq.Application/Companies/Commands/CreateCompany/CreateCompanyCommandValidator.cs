using FluentValidation;

namespace Logistiq.Application.Companies.Commands.CreateCompany;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Please provide a valid email address")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Website)
            .Must(BeAValidUrl).WithMessage("Please provide a valid website URL")
            .When(x => !string.IsNullOrEmpty(x.Website));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Phone must not exceed 20 characters");
    }

    private bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}