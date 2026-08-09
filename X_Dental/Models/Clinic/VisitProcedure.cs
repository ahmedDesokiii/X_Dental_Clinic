using Dental.Models.Common;
using Dental.Models.Patient;
using static Dental.Constants.EnumHelper;

namespace Dental.Models.Clinic
{
    // ========== VisitProcedure (الذهب) ==========
    public class VisitProcedure : BaseEntity
    {
        public int VisitId { get; set; }
        public Visit Visit { get; set; } = null!;
        public int ProcedureCatalogId { get; set; }
        public ProcedureCatalog Procedure { get; set; } = null!;
        public int? ToothNumber { get; set; }
        public string? Surface { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public ProcedureStatus Status { get; set; } = ProcedureStatus.Planned;
        public string? Notes { get; set; }
    }
}
