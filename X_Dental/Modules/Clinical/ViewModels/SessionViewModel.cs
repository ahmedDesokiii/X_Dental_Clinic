using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Patients.DTOs;

namespace Dental.Modules.Clinical.ViewModels
{
    public class SessionViewModel
    {
        public PatientDetailsDto Patient { get; set; } = null!;
        public VisitDto Visit { get; set; } = null!;
        public List<DentalChartEntry> DentalHistory { get; set; } = new();
        public List<DiagnosisCatalog> AvailableDiagnoses { get; set; } = new();
        public List<ProcedureCatalog> AvailableProcedures { get; set; } = new();

        // 📁 تجميع المدفوعات حسب نوع الملف — يتملى بس لو المريض "عام + تقويم"
        public List<CategoryPaymentSummaryDto> CategoryBreakdown { get; set; } = new();
        public bool ShowCategoryBreakdown => Patient.PatientType == PatientType.Both;

        // 💰 متبقي كل الزيارات السابقة للمريض (نفس نوع الزيارة الحالية — من غير الزيارة الحالية)
        // إجمالي المتبقي على المريض = PreviousBalance + متبقي اليوم
        public decimal PreviousBalance { get; set; }
    }

    public class CategoryPaymentSummaryDto
    {
        public string CategoryName { get; set; } = "";
        public string Icon { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining { get; set; }
    }
}