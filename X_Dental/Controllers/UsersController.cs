namespace Dental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
        }


        #region All Users
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.OrderByDescending(u => u.CurrentState).ToListAsync();
            var userVM = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user); 
                userVM.Add(new UserViewModel()
                {
                    Id = user.Id.ToString(),
                    Code = user.Code,
                    FullName_Ar = user.FullName_Ar,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    CurrentState = user.CurrentState,
                    Roles = roles
                });
            }
            return View(userVM);
        }
        #endregion

        #region User Process [Add & Update]
        [HttpGet]
        public async Task<IActionResult> AddUser(string userId)
        {
            var selectConn = HttpContext.Session.GetString("SelectedConnection");
            var connectionStrings = _configuration.GetSection("ConnectionStrings")
                                     .GetChildren()
                                     .Select(cs => cs.Key)
                                     .ToList();
            var roles = await _roleManager.Roles.Select(r => new CheckBoxViewModel { RoleId = r.Id.ToString(), DisplayValue = r.Name }).ToListAsync();
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addUserVM = new AddUserViewModel
            {
                Roles = roles,
                ConnectionNames = connectionStrings,
                SelectedConnection = selectConn
            };

            return View(addUserVM);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AddUserViewModel model)
        {
            if (ModelState.IsValid)
                return View(model);


            if (!model.Roles.Any(r => r.IsSelected))
            {
                ModelState.AddModelError("تحديد المجموعات", "فضلا , حدد علي الأقل مجموعة واحدة");
                return View(model);
            }

            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError("تكرار اسم المستخدم", "اسم المستخدم موجود مسبقاً");
                return View(model);
            }

            var user = new ApplicationUser()
            {
                Code = model.Code,
                Gender = model.Gender,
                PassUser = model.PassUser,
                EmailConfirmed = true,
                CurrentState = 2,
                FullName_Ar = model.FullName_Ar,
                PhoneNumber = model.PhoneNumber,
                UserName = model.UserName,
                Email = model.Email,
                
            };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("Roles", error.Description);
                }
                return View(model);
            }
            await _userManager.AddToRolesAsync(user, model.Roles.Where(r => r.IsSelected).OrderBy(r => r.IsSelected).Select(r => r.DisplayValue));
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            string usrId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (user == null)
                return NotFound();

            var profileFormVM = new ProfileFormViewModel
            {
                Id = userId,
                FullName_Ar = user.FullName_Ar,
                FullName_En = user.FullName_En,
                IdentityNum = user.IdentityNum,
                PhoneNumber = user.PhoneNumber,
                UserName = user.UserName,
                PassUser = user.PassUser,
                Email = user.Email
            };

            return View(profileFormVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(ProfileFormViewModel model)
        {
            if (ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);

            if (user == null)
                return NotFound();

            user.FullName_Ar = model.FullName_Ar;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Roles Process [Manage Roles]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var userRolesVM = new UserRolesViewModel
            {
                UserId = user.Id.ToString(),
                UserName = user.UserName,
                Roles = allRoles.Select(role => new CheckBoxViewModel
                {
                    RoleId = role.Id.ToString(),
                    DisplayValue = role.Name,
                    IsSelected = userRoles.Contains(role.Name) 
                }).ToList()
            };
            return View(userRolesVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in model.Roles)
            {
                if (userRoles.Any(r => r == role.DisplayValue) && !role.IsSelected)
                    await _userManager.RemoveFromRoleAsync(user, role.DisplayValue);

                if (!userRoles.Any(r => r == role.DisplayValue) && role.IsSelected)
                    await _userManager.AddToRoleAsync(user, role.DisplayValue);
            }

            var testRole = await _userManager.GetRolesAsync(user);
            if (testRole.Count == 0)
            {
                user.CurrentState = 1;
                user.EmailConfirmed = false;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                user.CurrentState = 2;
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRoles(UserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, userRoles);
            await _userManager.AddToRolesAsync(user, model.Roles.Where(r => r.IsSelected).Select(r => r.DisplayValue));

            return RedirectToAction(nameof(Index));
        }
        #endregion
        [HttpGet]
        public IActionResult GetUserName(int userId)
        {
            var name = _context.Users
                         .Where(u => u.Id == userId)
                         .Select(u => u.FullName_Ar)
                         .FirstOrDefault();
            return Json(new { userName = name });
        }


        #region Delete User
        public async Task<IActionResult> DeleteUser(int Id)
        {
            var user = await _userManager.FindByIdAsync(Id.ToString());

            if (user == null)
                return NotFound();

            user.CurrentState = 0;
            user.EmailConfirmed = false;

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));

        }
        #endregion
    }
}
