using Dental.Models.Common;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;

namespace Dental.Models.Clinic
{
    // ========== VisitPayment (مرتبطة بالإجراء) ==========
    public class VisitPayment : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }   // ⭐ جديد
        public PaymentMethod Method { get; set; }
        public string? Description { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string? ReceiptNo { get; set; }
    }
}
