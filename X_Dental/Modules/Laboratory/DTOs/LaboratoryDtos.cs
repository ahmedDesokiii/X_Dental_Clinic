namespace Dental.Modules.Laboratory.DTOs
{
    public class LaboratoryDiagnosisDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string NameAr { get; set; } = null!;
        public decimal CurrentPrice { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; }
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