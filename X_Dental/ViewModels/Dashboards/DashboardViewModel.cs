// ============================================================
// 📁 File: ViewModels/Dashboard/DashboardViewModel.cs
// ============================================================
using Dental.Models.Patient;

namespace Dental.ViewModels.Dashboards
{
    public class DashboardViewModel
    {
        public int GeneralPatients { get; set; }
        public int OrthoPatients { get; set; }
        public int BothPatients { get; set; }

        // ---- بطاقات الأرقام ----
        public int TotalPatients { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int TotalVisits { get; set; }
        public int TodayAppointments { get; set; }
        public int UpcomingAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDue { get; set; }
        public int GeneralVisits { get; set; }
        public int OrthoVisits { get; set; }

        // ---- الجداول ----
        public List<RecentAppointmentVM> RecentAppointments { get; set; } = new();
        public List<RecentVisitVM> RecentVisits { get; set; } = new();
        public List<TopProcedureVM> TopProcedures { get; set; } = new();

        // ---- الجرافات ----
        public List<DateCountPoint> VisitsLast7Days { get; set; } = new();
        public List<DateAmountPoint> RevenueLast6Months { get; set; } = new();
        public List<DateCountPoint> AppointmentNext7Days { get; set; } = new();
    }

    public class RecentAppointmentVM
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public DateTime AppointmentDate { get; set; }
        public string? Reason { get; set; }
        public AppointmentStatus Status { get; set; }
    }

    public class RecentVisitVM
    {
        public int Id { get; set; }
        public string PatientName { get; set; } = "";
        public DateTime VisitDate { get; set; }
        public string? ChiefComplaint { get; set; }
        public string? Category { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }

    public class TopProcedureVM
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DateCountPoint
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public class DateAmountPoint
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
    }
}