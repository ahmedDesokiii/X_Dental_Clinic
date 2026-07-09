namespace SchoolPsychologist.ViewModels.Dashboard
{
    public class PsychologyDashboardViewModel
    {
        // KPIs
        public int TotalCases { get; set; }
        public int OpenCases { get; set; }
        public int ClosedCases { get; set; }

        public int TotalReports { get; set; }

        // Alerts
        public bool HasCriticalCases { get; set; }
        public bool HasPendingReports { get; set; }
        public bool HasMissingFollowUp { get; set; }

        // Latest Data
        public List<PsychologyCaseDto> LatestCases { get; set; }
        public List<ReportDto> LatestReports { get; set; }

        // Charts
        public List<ChartSummary> CasesByStatus { get; set; }
        public List<ChartSummary> CasesByType { get; set; }
    }
}
