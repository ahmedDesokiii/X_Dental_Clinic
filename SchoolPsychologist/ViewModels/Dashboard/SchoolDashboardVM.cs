namespace SchoolPsychologist.ViewModels.Dashboard
{
    public class SchoolDashboardVM
    {
        // KPI
        public int TotalStudents { get; set; }
        public int TotalClasses { get; set; }
        public int TotalTeacherReports { get; set; }
        public int TotalPsyReports { get; set; }
        public int TotalSocialReports { get; set; }
        public int TodayAttendance { get; set; }
        public int AbsentToday { get; set; }

        // Alerts
        public bool HasHighAbsence { get; set; }
        public bool HasCriticalCases { get; set; }

        // Charts
        public List<ChartSummary> StudentsByClass { get; set; } = new();
        public List<ChartSummary> AttendanceOverTime { get; set; } = new();
        public List<ChartSummary> ReportsByType { get; set; } = new();
        public List<ChartSummary> CasesSeverity { get; set; } = new();

        // Latest
        public List<Student> LatestStudents { get; set; } = new();
    }
}
