namespace SchoolPsychologist.ViewModels.User
{
    public class AddUserViewModel
    {
        public List<string> ConnectionNames { get; set; }
        public string SelectedConnection { get; set; }
        public string Id { get; set; }
        [Required, MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(200), MinLength(5)]
        public string FullName_Ar { get; set; } = string.Empty;

     
       
        public string Gender { get; set; } = string.Empty;

        [Phone]
        [Required]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }

        public string PassUser { get; set; } = string.Empty;

        [Required]
        //[StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [Display(Name = "Password")]
        public string Password { get; set; }


        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public List<CheckBoxViewModel>? Roles { get; set; }
        
    }
}
