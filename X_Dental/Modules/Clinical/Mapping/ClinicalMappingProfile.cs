using AutoMapper;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.Mapping
{
    public class ClinicalMappingProfile : Profile
    {
        public ClinicalMappingProfile()
        {
            // Visit → VisitDto
            CreateMap<Visit, VisitDto>()
                .ForMember(dest => dest.Remaining, opt => opt.MapFrom(src => src.Remaining));

            // VisitDiagnosis → VisitDiagnosisDto
            CreateMap<VisitDiagnosis, VisitDiagnosisDto>()
                .ForMember(dest => dest.DiagnosisName, opt => opt.MapFrom(src => src.Diagnosis.NameAr))
                .ForMember(dest => dest.DiagnosisCode, opt => opt.MapFrom(src => src.Diagnosis.Code));

            // VisitProcedure → VisitProcedureDto
            CreateMap<VisitProcedure, VisitProcedureDto>()
                .ForMember(dest => dest.ProcedureName, opt => opt.MapFrom(src => src.Procedure.NameAr))
                .ForMember(dest => dest.ProcedureCode, opt => opt.MapFrom(src => src.Procedure.Code))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

            // VisitPayment → VisitPaymentDto
            CreateMap<VisitPayment, VisitPaymentDto>();

            // PatientXRay → XRayDto (لو محتاجه)
            CreateMap<PatientXRay, XRayDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => $"/uploads/xrays/{src.FileName}"));

            // Prescription → PrescriptionDto (من الـ DTOs الموجودة)
            CreateMap<Prescription, PrescriptionDto>();
        }
    }
}