// ============================================================
// 📁 File: Modules/Payments/DTOs/PaymentDtos.cs
// ============================================================
using Dental.Modules.Patients.DTOs; // PaymentMethod, PaymentStatus معرّفين هنا في مشروعك أصلاً
using Dental.Modules.Patients.Enums;

namespace Dental.Modules.Payments.DTOs
{
    // ── ملخص زيارة واحدة (يستخدم في كشف الحساب) ──
    public class VisitSummaryDto
    {
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public VisitCategory Category { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Remaining { get; set; }
        public bool IsFullyPaid => Remaining <= 0;
    }

    // ── سجل دفعة واحدة (يستخدم في كشف الحساب) ──
    public class PaymentRecordDto
    {
        public int Id { get; set; }
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string MethodText { get; set; } = null!;
        public string? Description { get; set; }
        public string? ReceiptNo { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    // ── كشف الحساب الشامل للمريض (كل الزيارات مع بعض) ──
    public class PatientStatementDto
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = null!;
        public string PatientMobile { get; set; } = null!;
        public string PatientCode { get; set; } = null!;

        public decimal TotalAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        public List<VisitSummaryDto> Visits { get; set; } = new();
        public List<PaymentRecordDto> Payments { get; set; } = new();
    }

    // ── تسجيل دفعة جديدة ──
    public class AddPaymentDto
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string? Description { get; set; }
    }

    // ── نتيجة تسجيل الدفعة (يحتاجها الفرونت يحدّث الأرقام فورًا من غير Reload) ──
    public class AddPaymentResultDto
    {
        public int PaymentId { get; set; }
        public string? ReceiptNo { get; set; }
        public decimal VisitPaidAmount { get; set; }
        public decimal VisitRemaining { get; set; }
    }
}
