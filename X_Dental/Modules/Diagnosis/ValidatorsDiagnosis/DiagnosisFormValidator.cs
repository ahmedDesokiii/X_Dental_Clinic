// ============================================================
// 📁 File: Modules/Diagnosis/ValidatorsDiagnosis/DiagnosisFormValidator.cs
// ============================================================
using Dental.Modules.Diagnosis.DTOs;
using FluentValidation;

namespace Dental.Modules.Diagnosis.ValidatorsDiagnosis
{
    public class DiagnosisFormValidator : AbstractValidator<DiagnosisFormDto>
    {
        public DiagnosisFormValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("كود التشخيص مطلوب.")
                .MaximumLength(20).WithMessage("كود التشخيص لا يجب أن يتجاوز 20 حرف.");

            RuleFor(x => x.NameAr)
                .NotEmpty().WithMessage("اسم التشخيص بالعربي مطلوب.")
                .MaximumLength(200);

            RuleFor(x => x.NameEn)
                .MaximumLength(200);

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون بالسالب.");

            RuleFor(x => x.Description)
                .MaximumLength(500);

            RuleFor(x => x.Notes)
                .MaximumLength(1000);
        }
    }
}