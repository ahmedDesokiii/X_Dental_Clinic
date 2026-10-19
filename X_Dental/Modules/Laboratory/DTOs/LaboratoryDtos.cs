namespace Dental.Modules.Laboratory.DTOs
{
    public class LaboratoryDiagnosisDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
        /// <summary>
        /// مجموع أسعار المعمل للحالات داخل الفترة المحددة
        /// (LabPriceSnapshot إن وُجد، وإلا آخر سعر من Edit Price) — لكل وحدة (سن).
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>عدد الحالات داخل الفترة المحددة (مجموعة Multi-Unit = حالة واحدة).</summary>
        public int CasesCount { get; set; }

        /// <summary>⭐ عدد الوحدات (الأسنان) داخل الفترة المحددة.</summary>
        public int UnitsCount { get; set; }

        /// <summary>الحالات (المرضى) اللي اتعملهم التشخيص ده داخل الفترة المحددة.</summary>
        public List<LaboratoryCaseDto> Cases { get; set; } = new();
    }

    public class LaboratoryCaseDto
    {
        public int VisitId { get; set; }
        public DateTime VisitDate { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string? Mobile { get; set; }

        // ⭐ Multi-Unit
        /// <summary>عدد الوحدات (الأسنان) في الحالة دي.</summary>
        public int Units { get; set; } = 1;
        /// <summary>الأسنان مرتبة على امتداد الفك.</summary>
        public List<int> Teeth { get; set; } = new();
        public bool IsMultiUnit { get; set; }
        /// <summary>مبلغ المعمل للحالة = مجموع LabPriceSnapshot لكل وحدة.</summary>
        public decimal Amount { get; set; }
    }

    /// <summary>⭐ الجدول + الـ KPIs في Response واحد (بدل GetDiagnoses + GetSummary).</summary>
    public class LaboratoryDashboardDto
    {
        public List<LaboratoryDiagnosisDto> Diagnoses { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalCases { get; set; }
        public int TotalUnits { get; set; }
        public int DiagnosisTypes { get; set; }
    }

    public class LaboratoryPriceHistoryDto
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public DateTime EffectiveDate { get; set; }
        public string? ChangedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLaboratoryPriceRequest
    {
        public int DiagnosisCatalogId { get; set; }
        public decimal NewPrice { get; set; }
        public string? Notes { get; set; }
    }

    public class LaboratorySummaryItemDto
    {
        public int DiagnosisCatalogId { get; set; }
        public string NameAr { get; set; } = null!;
        public string NameEn { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Total { get; set; }
    }

    public class LaboratorySummaryDto
    {
        public List<LaboratorySummaryItemDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
    }

    public class LaboratoryFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? DiagnosisCatalogId { get; set; }
        public int? PatientId { get; set; }
    }
}