// ============================================================
// 📁 File: Modules/Payments/DTOs/DailyPaymentsReportDtos.cs
// ⭐ ملف جديد — متلمسش ملف الـ DTOs الحالي، ده ملف مستقل بنفس الـ namespace
// ============================================================
namespace Dental.Modules.Payments.DTOs
{
    public class DailyPaymentsReportDto
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public int Days { get; set; }

        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public decimal AveragePayment { get; set; }

        public decimal GeneralAmount { get; set; }
        public int GeneralCount { get; set; }
        public decimal OrthoAmount { get; set; }
        public int OrthoCount { get; set; }

        // إجماليات الزيارات اللي عليها دفعات في الفترة (كل زيارة مرة واحدة)
        public int VisitsCount { get; set; }
        public decimal VisitsTotal { get; set; }
        public decimal VisitsPaid { get; set; }
        public decimal VisitsRemaining { get; set; }

        public int GeneralVisitsCount { get; set; }
        public decimal GeneralVisitsTotal { get; set; }
        public decimal GeneralVisitsPaid { get; set; }
        public decimal GeneralVisitsRemaining { get; set; }

        public int OrthoVisitsCount { get; set; }
        public decimal OrthoVisitsTotal { get; set; }
        public decimal OrthoVisitsPaid { get; set; }
        public decimal OrthoVisitsRemaining { get; set; }

        // مقارنة بنفس طول الفترة اللي قبلها مباشرة
        public decimal PreviousTotalAmount { get; set; }
        public int PreviousTotalCount { get; set; }
        public decimal? ChangePercent { get; set; }

        public List<DailyPaymentsDayDto> ByDay { get; set; } = new();
        public List<DailyPaymentsHourDto> ByHour { get; set; } = new();
        public List<DailyPaymentsMethodDto> ByMethod { get; set; } = new();
        public List<DailyPaymentItemDto> Payments { get; set; } = new();
    }

    public class DailyPaymentsDayDto
    {
        public DateTime Date { get; set; }
        public decimal GeneralAmount { get; set; }
        public decimal OrthoAmount { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class DailyPaymentsHourDto
    {
        public int Hour { get; set; }
        public decimal GeneralAmount { get; set; }
        public decimal OrthoAmount { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class DailyPaymentsMethodDto
    {
        public int Method { get; set; }
        public string MethodText { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class DailyPaymentItemDto
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? PatientMobile { get; set; }
        public bool IsOrtho { get; set; }
        public string CategoryText { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Method { get; set; }
        public string MethodText { get; set; } = string.Empty;
        public string? ReceiptNo { get; set; }
        public string? Description { get; set; }
        public DateTime PaymentDate { get; set; }

        // أرقام الزيارة المرتبطة بالدفعة (قيمها الحالية)
        public decimal VisitTotal { get; set; }
        public decimal VisitDiscount { get; set; }
        public decimal VisitPaid { get; set; }
        public decimal VisitRemaining { get; set; }
    }
}