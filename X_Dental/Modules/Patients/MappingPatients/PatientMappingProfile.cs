// ============================================================
// 📁 File: PatientMappingProfile.cs (AutoMapper)
// ============================================================

using AutoMapper;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // ── Entity → DTO ──
        CreateMap<Patient, PatientDto>();

        CreateMap<Patient, PatientDetailsDto>()
            .ForMember(dest => dest.Allergies,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.Allergies : null))
            .ForMember(dest => dest.ChronicDiseases,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.ChronicDiseases : null))
            .ForMember(dest => dest.CurrentMedications,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.CurrentMedications : null))
            // 🐞 B1: الحقول دي ما كانتش بتتقري من MedicalInfo، فشاشة التعديل كانت بتفتح فاضية
            //        وأول حفظ كان بيمسح التدخين/الحمل/الملاحظات الطبية/التحذيرات
            .ForMember(dest => dest.SmokingStatus,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.SmokingStatus : null))
            .ForMember(dest => dest.MedicalNotes,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.MedicalNotes : src.Notes))
            .ForMember(dest => dest.IsPregnant,
                opt => opt.MapFrom(src => src.MedicalInfo != null && src.MedicalInfo.IsPregnant))
            .ForMember(dest => dest.EmergencyNotes,
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.EmergencyNotes : null));

        CreateMap<Patient, PatientLookupDto>();

        // ── DTO → Entity ──
        CreateMap<CreatePatientDto, Patient>();
        CreateMap<UpdatePatientDto, Patient>()
        .ForMember(dest => dest.GenderId, opt => opt.MapFrom(src => src.GenderId))
        .ForMember(dest => dest.BloodGroupId, opt => opt.MapFrom(src => src.BloodGroupId));

        // ── Related Entities → DTOs ──
        CreateMap<Gender, GenderDto>();
        CreateMap<BloodGroup, BloodGroupDto>();
    }
}