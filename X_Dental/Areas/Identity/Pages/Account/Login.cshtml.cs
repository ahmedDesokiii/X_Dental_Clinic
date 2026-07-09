// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dental.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LoginModel> logger, IEmailSender emailSender, ApplicationDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }


        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        [BindProperty(SupportsGet = true)] // Allows the property to be bound on GET requests

        public string SelectedConnection { get; set; } = "DefaultConnection";

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>

            [EmailAddress]
            public string Email { get; set; }

            [Required]
            public string UserName { get; set; }
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            //[Required]

            public int? CurrentState { get; set; }
            public string? SelectedConnection { get; set; }
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            var test = SelectedConnection;
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            // Get External Logins
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            //Get Connection Names from Configuration
            IConfigurationSection connectionsSection = _configuration.GetSection("ConnectionStrings");

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            if (SelectedConnection != "DefaultConnection")
            {
                var selectedConnectionsSection = _configuration.GetConnectionString(test);
                //Retrive Selected Connection From OnPost
                var cc = _context.GetService<IRelationalConnection>().ConnectionString = selectedConnectionsSection;
            }

            //Set Return URL
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostSetConnectionString(string returnUrl = null)
        {
            // set connection string in session or cookie
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            // Set Connection String
            var selectedConnectionsSection = _configuration.GetConnectionString(Input.SelectedConnection);

            if (selectedConnectionsSection != null)
            {
                _context.GetService<IRelationalConnection>().ConnectionString = selectedConnectionsSection;
            }
            var selectedConn = Input.SelectedConnection;
            returnUrl ??= Url.Content("./login");
            //Set Return URL
            ReturnUrl = returnUrl;
            // ...
            return RedirectToPage(returnUrl, new { SelectedConnection = selectedConn }); // Stay on the page or redirect
        }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Home/index");

            var connectionString = _configuration.GetConnectionString(SelectedConnection);

            //Use SqlConnectionStringBuilder to safely extract properties
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
            builder.InitialCatalog = builder.InitialCatalog; // Database name
            builder.ConnectionString = connectionString;
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(builder.ConnectionString);
            var selectConn = new ApplicationDbContext(optionsBuilder.Options);
    
            if (ModelState.IsValid)
            {
                _context.GetService<IRelationalConnection>().ConnectionString = connectionString;
                
                var user = await _userManager.FindByNameAsync(Input.UserName);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "خطأ في ادخال اسم المستخدم او كلمة المرور  !");
                    returnUrl ??= Url.Content("~/");
                    return Page();
                }

                if (user != null)
                {
                    if (user.CurrentState == 1)
                    {
                        ModelState.AddModelError(string.Empty, "المستخدم معلق , يرجى مراجعة مدير النظام !");
                        returnUrl ??= Url.Content("~/");
                        return Page();
                    }
                }

                // Save selected connection in session
                if (!string.IsNullOrEmpty(SelectedConnection))
                {
                    HttpContext.Session.SetString("SelectedConnection", SelectedConnection);
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.UserName, Input.Password, true, lockoutOnFailure: false);


                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    _context.Users.Update(user);
                    _context.SaveChanges();

                    return LocalRedirect(returnUrl);
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "خطأ في ادخال اسم المستخدم أو كلمة المرور !");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

    }
}
