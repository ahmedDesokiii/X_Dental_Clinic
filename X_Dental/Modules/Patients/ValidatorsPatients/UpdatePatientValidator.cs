using Dental.Modules.Patients.DTOs;
using FluentValidation;

public class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.FullNameAr)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(20);

        RuleFor(x => x.Mobile)
            .MaximumLength(20);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.NationalId)
            .Length(14)
            .When(x => !string.IsNullOrWhiteSpace(x.NationalId));

        RuleFor(x => x.GenderId)
            .GreaterThan(0);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}