// ============================================================
// 📁 File: CreatePatientValidator.cs (Updated)
// ============================================================

using Dental.Modules.Patients.DTOs;
using FluentValidation;

namespace Dental.Modules.Patients.ValidatorsPatients
{
    public class CreatePatientValidator : AbstractValidator<CreatePatientDto>
    {
        public CreatePatientValidator()
        {
            RuleFor(x => x.FullNameAr)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MaximumLength(200)
                .WithMessage("Full name must not exceed 200 characters.");

            RuleFor(x => x.Phone)
                .MaximumLength(20)
                .WithMessage("Phone must not exceed 20 characters.");

            RuleFor(x => x.Mobile)
                .MaximumLength(20)
                .WithMessage("Mobile must not exceed 20 characters.");

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("Invalid email format.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.NationalId)
                .Length(14)
                .WithMessage("National ID must be exactly 14 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId));

            // ✅ Changed from Gender (enum) to GenderId (int)
            RuleFor(x => x.GenderId)
                .GreaterThan(0)
                .WithMessage("Gender is required.");

            RuleFor(x => x.Address)
                .MaximumLength(500)
                .WithMessage("Address must not exceed 500 characters.");

            RuleFor(x => x.Notes)
                .MaximumLength(2000)
                .WithMessage("Notes must not exceed 2000 characters.");
        }
    }
}