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
                opt => opt.MapFrom(src => src.MedicalInfo != null ? src.MedicalInfo.CurrentMedications : null));

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