// ============================================================
// 📁 File: Dental.Modules.Patients.DTOs (All DTOs)
// ============================================================

using Dental.Models.Patient;

namespace Dental.Modules.Patients.DTOs
{
    // ── Gender DTO ──
    public class GenderDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
    }

    // ── BloodGroup DTO ──
    public class BloodGroupDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    // ── Patient DTO (For List/Index) ──
    public class PatientDto
    {
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public GenderDto? Gender { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public int Age { get; set; }
    }

    // ── Patient Details DTO (For Dashboard) ──
    public class PatientDetailsDto
    {
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public int Age => DateOfBirth.HasValue
            ? DateTime.Now.Year - DateOfBirth.Value.Year
            : 0;
        public GenderDto? Gender { get; set; }
        public BloodGroupDto? BloodGroup { get; set; }
        public string? NationalId { get; set; }
        public string? Occupation { get; set; }
        public string? Mobile { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Medical Info (from PatientMedicalInfo navigation)
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? SmokingStatus { get; set; }
        public string? MedicalNotes { get; set; }
        public bool IsPregnant { get; set; }
        public string? EmergencyNotes { get; set; }

        // Dental History
        public DateTime? LastVisit { get; set; }
        public DateTime? NextAppointment { get; set; }


        public int VisitsCount { get; set; }
        public int GeneralVisitsCount { get; set; }
        public int OrthodonticsVisitsCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue { get; set; }
        public DateTime? NextVisitDate { get; set; }


        public PatientType PatientType { get; set; }
    }

    // ── Patient Lookup DTO (For Search/Dropdowns) ──
    public class PatientLookupDto
    {
        public int Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullNameAr { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    // ── Create Patient DTO ──
    public class CreatePatientDto
    {
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }
        public int? GenderId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? NationalId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public int? BloodGroupId { get; set; }
        public string? Notes { get; set; }

        public PatientType PatientType { get; set; } = PatientType.General;

        // Medical Info
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? MedicalNotes { get; set; }
        public string? SmokingStatus { get; set; }
        public bool IsPregnant { get; set; }
        public string? EmergencyNotes { get; set; }
    }

    // ── Update Patient DTO ──
    public class UpdatePatientDto
    {
        public int Id { get; set; }
        public string FullNameAr { get; set; } = string.Empty;
        public string? FullNameEn { get; set; }

        public int? GenderId { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public int? BloodGroupId { get; set; }
        public string? NationalId { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Occupation { get; set; }
        public string? Notes { get; set; }

        public PatientType PatientType { get; set; }

        // ⭐ Medical Info - جديد
        public string? Allergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public string? CurrentMedications { get; set; }
        public string? MedicalNotes { get; set; }
        public string? SmokingStatus { get; set; }
        public bool IsPregnant { get; set; }
        public string? EmergencyNotes { get; set; }

        public static explicit operator UpdatePatientDto(CreatePatientDto v)
        {
            throw new NotImplementedException();
        }
    }

}