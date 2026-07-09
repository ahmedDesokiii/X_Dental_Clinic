namespace Dental.ViewModels.User
{
    public class UserViewModel
    {

        public string Id { get; set; } = string.Empty;
        public string Company_Shortcut { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string FullName_Ar { get; set; }
        public string FullName_En { get; set; } = string.Empty;
        public string IdentityNum { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Password_Signature { get; set; } = string.Empty;
        //نشط : 2  & معلق : 1  & غير نشط : 0 
        public int? CurrentState { get; set; }
        public IEnumerable<string> Roles { get; set; }
        public IEnumerable<string> FinancialPeriods { get; set; }

        public string? SelectedFinancialYear { get; set; }

    }
}
