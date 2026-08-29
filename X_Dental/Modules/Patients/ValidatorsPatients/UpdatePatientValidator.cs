using Dental.Modules.Patients.DTOs;
using FluentValidation;

namespace Dental.Modules.Patients.ValidatorsPatients
{
    public class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
    {
        public UpdatePatientValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);

            RuleFor(x => x.FullNameAr)
                .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
                .MaximumLength(200).WithMessage("الاسم لا يتجاوز 200 حرف.");

            RuleFor(x => x.Mobile)
                .NotEmpty().WithMessage("رقم الموبايل مطلوب.")
                .MaximumLength(20).WithMessage("رقم الموبايل لا يتجاوز 20 رقم.");

            RuleFor(x => x.Phone)
                .MaximumLength(20).WithMessage("رقم التليفون لا يتجاوز 20 رقم.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("صيغة البريد غير صحيحة.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.NationalId)
                .Length(14).WithMessage("الرقم القومي يجب أن يكون 14 رقم.")
                .When(x => !string.IsNullOrWhiteSpace(x.NationalId));

            
            RuleFor(x => x.Address)
                .MaximumLength(500).WithMessage("العنوان لا يتجاوز 500 حرف.");

            RuleFor(x => x.Notes)
                .MaximumLength(2000).WithMessage("الملاحظات لا تتجاوز 2000 حرف.");

            // Medical Info Validation
            RuleFor(x => x.Allergies)
                .MaximumLength(500).WithMessage("الحساسية لا تتجاوز 500 حرف.");

            RuleFor(x => x.ChronicDiseases)
                .MaximumLength(500).WithMessage("الأمراض المزمنة لا تتجاوز 500 حرف.");

            RuleFor(x => x.CurrentMedications)
                .MaximumLength(500).WithMessage("الأدوية الحالية لا تتجاوز 500 حرف.");

            RuleFor(x => x.MedicalNotes)
                .MaximumLength(2000).WithMessage("الملاحظات الطبية لا تتجاوز 2000 حرف.");

            RuleFor(x => x.EmergencyNotes)
                .MaximumLength(1000).WithMessage("ملاحظات الطوارئ لا تتجاوز 1000 حرف.");
        }
    }
}
