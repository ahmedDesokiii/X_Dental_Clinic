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
        public decimal Discount { get; set; }      // ⭐ المرحلة 5
        public PaymentMethod Method { get; set; }
        public string MethodText { get; set; } = null!;
        public string? Description { get; set; }
        public string? ReceiptNo { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    // ⭐ المرحلة 5 — الدفعة كما دفعها المريض فعلًا (إيصال واحد)
    // حتى لو اتوزعت على أكتر من زيارة (FIFO) بتظهر كسطر واحد بالإجمالي، والتفاصيل في Parts
    public class ReceiptGroupDto
    {
        public string Key { get; set; } = string.Empty;
        public int PaymentId { get; set; }                // أي جزء منها — الإلغاء بيلغي الإيصال كله
        public string? ReceiptNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        public PaymentMethod Method { get; set; }
        public string MethodText { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsOrtho { get; set; }
        public bool IsRefund => Amount < 0;
        public List<ReceiptPartDto> Parts { get; set; } = new();
    }

    public class ReceiptPartDto
    {
        public int PaymentId { get; set; }
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }
        public bool IsArrears { get; set; }               // سداد متأخرات (زيارة أقدم)
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
        public List<PaymentRecordDto> Payments { get; set; } = new();     // كل حركة لوحدها (متسابة للتوافق)
        public List<ReceiptGroupDto> Receipts { get; set; } = new();      // ⭐ المرحلة 5 — الدفعات مجمّعة بالإيصال
    }

    // ── تسجيل دفعة جديدة ──
    public class AddPaymentDto
    {
        public int VisitId { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }      // ⭐ المرحلة 5 — نفس منطق السيشن
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        public string? Description { get; set; }
    }

    // ⭐ المرحلة 5 — نتيجة توزيع دفعة واحدة (محرك الدفع الموحّد)
    public class PaymentAllocationResult
    {
        public string ReceiptNo { get; set; } = string.Empty;
        public decimal ToTarget { get; set; }       // اللي راح على الزيارة المختارة (شامل أي رصيد دائن)
        public decimal SettledOld { get; set; }     // اللي سدد زيارات أقدم
        public decimal Credit { get; set; }         // زيادة عن كل المطلوب (رصيد دائن على الزيارة المختارة)
        public int VisitsTouched { get; set; }
    }

    // ── نتيجة تسجيل الدفعة (يحتاجها الفرونت يحدّث الأرقام فورًا من غير Reload) ──
    public class AddPaymentResultDto
    {
        public int PaymentId { get; set; }
        public string? ReceiptNo { get; set; }
        public decimal VisitPaidAmount { get; set; }
        public decimal VisitRemaining { get; set; }
        public decimal SettledOld { get; set; }     // ⭐ المرحلة 5
        public decimal Credit { get; set; }
        public int VisitsTouched { get; set; }
    }
}
