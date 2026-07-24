// ============================================================
// 📁 File: Modules/Users/DTOs/UserListDto.cs
// ============================================================

namespace Dental.Modules.Users.DTOs
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? FullName_Ar { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public int CurrentState { get; set; }
        public List<string> Roles { get; set; } = new();

        // Computed property for DataTables rendering
        public string StateBadge => CurrentState switch
        {
            2 => "<span class=\"badge bg-success-subtle text-success fw-bold px-2 py-1 rounded-5\"><i class=\"bi bi-check-circle-fill\"></i> نشط</span>",
            1 => "<span class=\"badge bg-warning-subtle text-warning fw-bold px-2 py-1 rounded-5\"><i class=\"bi bi-pause-circle-fill\"></i> معلق</span>",
            _ => "<span class=\"badge bg-danger-subtle text-danger fw-bold px-2 py-1 rounded-5\"><i class=\"bi bi-x-circle-fill\"></i> غير نشط</span>"
        };

        public string StateText => CurrentState switch
        {
            2 => "نشط",
            1 => "معلق",
            _ => "غير نشط"
        };
    }
}