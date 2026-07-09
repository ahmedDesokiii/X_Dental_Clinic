
namespace Dental.Models.User
{
    public class ApplicationUser : IdentityUser<int>
    {
        
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200), MinLength(5)]
        public string FullName_Ar { get; set; } = string.Empty;

        [MaxLength(200), MinLength(5)]
        public string FullName_En { get; set; } = string.Empty;

        [Required, MaxLength(20), MinLength(8)]
        public string IdentityNum { get; set; } = string.Empty;
        [MaxLength(10)]
        public string Gender { get; set;} = string.Empty;

        //password show
        public string PassUser { get; set; } = string.Empty;

        // نشط : 2  & غير نشط : 0 & معلق : 1
        public int? CurrentState { get; set; } = 0;

    }
}
