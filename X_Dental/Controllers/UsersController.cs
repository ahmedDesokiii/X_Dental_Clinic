// ============================================================
// 📁 File: Controllers/UsersController.cs (FINAL - WORKING)
// ============================================================

using Dental.Models.DataTables;
using Dental.Modules.Users.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

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
        private readonly IMemoryCache _cache;

        public UsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
            _cache = cache;
        }

        #region Index (Legacy - N+1 FIXED)
        public async Task<IActionResult> Index()
        {
            // Step 1: Fetch all users (single query)
            var users = await _context.Users
                .AsNoTracking()
                .OrderByDescending(u => u.CurrentState)
                .ToListAsync();

            // Step 2: Fetch ALL user-role mappings in ONE query
            var userIds = users.Select(u => u.Id).ToList();

            var userRoleMappings = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => userIds.Contains(ur.UserId))
                .ToListAsync();

            // Step 3: Fetch ALL roles in ONE query
            var roleIds = userRoleMappings.Select(ur => ur.RoleId).Distinct().ToList();

            var roles = await _context.Roles
                .AsNoTracking()
                .Where(r => roleIds.Contains(r.Id))
                .ToListAsync();

            // Step 4: Build lookup dictionaries in memory (no more DB queries!)
            var roleNamesById = roles.ToDictionary(r => r.Id, r => r.Name ?? "");

            var rolesByUserId = userRoleMappings
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur => roleNamesById.ContainsKey(ur.RoleId)
                        ? roleNamesById[ur.RoleId]
                        : "").Where(name => !string.IsNullOrEmpty(name)).ToList()
                );

            // Step 5: Build ViewModels
            var userVM = users.Select(u => new UserViewModel
            {
                Id = u.Id.ToString(),
                Code = u.Code,
                FullName_Ar = u.FullName_Ar,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                CurrentState = u.CurrentState,
                Roles = rolesByUserId.ContainsKey(u.Id) ? rolesByUserId[u.Id] : new List<string>()
            }).ToList();

            return View(userVM);
        }
        #endregion

        #region 🚀 DataTables Server-Side Processing
        [HttpPost]
        public async Task<IActionResult> GetUsersData([FromBody] DataTableRequest request)
        {
            // Step 1: Build base query
            var query = _context.Users.AsNoTracking().AsQueryable();

            // Step 2: Global Search
            if (!string.IsNullOrWhiteSpace(request.Search?.Value))
            {
                var search = request.Search.Value.Trim().ToLower();
                query = query.Where(u =>
                    (u.Code != null && u.Code.ToLower().Contains(search)) ||
                    (u.FullName_Ar != null && u.FullName_Ar.Contains(search)) ||
                    (u.UserName != null && u.UserName.ToLower().Contains(search)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search))
                );
            }

            var recordsTotal = await _context.Users.CountAsync();
            var recordsFiltered = await query.CountAsync();

            // Step 3: Sorting
            if (request.Order.Any() && request.Columns.Any())
            {
                var order = request.Order.First();
                var columnName = request.Columns[order.Column].Data;
                var isAsc = order.Dir == "asc";

                query = columnName switch
                {
                    "code" => isAsc ? query.OrderBy(u => u.Code) : query.OrderByDescending(u => u.Code),
                    "fullNameAr" => isAsc ? query.OrderBy(u => u.FullName_Ar) : query.OrderByDescending(u => u.FullName_Ar),
                    "userName" => isAsc ? query.OrderBy(u => u.UserName) : query.OrderByDescending(u => u.UserName),
                    "phoneNumber" => isAsc ? query.OrderBy(u => u.PhoneNumber) : query.OrderByDescending(u => u.PhoneNumber),
                    "currentState" => isAsc ? query.OrderBy(u => u.CurrentState) : query.OrderByDescending(u => u.CurrentState),
                    _ => query.OrderByDescending(u => u.CurrentState)
                };
            }
            else
            {
                query = query.OrderByDescending(u => u.CurrentState);
            }

            // Step 4: Get paged users
            var pagedUsers = await query
                .Skip(request.Start)
                .Take(request.Length)
                .ToListAsync();

            // Step 5: Get roles for paged users ONLY (3 queries total, not N+1)
            var pagedUserIds = pagedUsers.Select(u => u.Id).ToList();

            var pagedUserRoleMappings = await _context.UserRoles
                .AsNoTracking()
                .Where(ur => pagedUserIds.Contains(ur.UserId))
                .ToListAsync();

            var pagedRoleIds = pagedUserRoleMappings.Select(ur => ur.RoleId).Distinct().ToList();

            var pagedRoles = await _context.Roles
                .AsNoTracking()
                .Where(r => pagedRoleIds.Contains(r.Id))
                .ToListAsync();

            // Step 6: Build lookups
            var pagedRoleNamesById = pagedRoles.ToDictionary(r => r.Id, r => r.Name ?? "");

            var pagedRolesByUserId = pagedUserRoleMappings
                .GroupBy(ur => ur.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ur => pagedRoleNamesById.ContainsKey(ur.RoleId)
                        ? pagedRoleNamesById[ur.RoleId]
                        : "").Where(name => !string.IsNullOrEmpty(name)).ToList()
                );

            // Step 7: Build DTOs
            var data = pagedUsers.Select(u => new UserListDto
            {
                Id = u.Id,
                Code = u.Code,
                FullName_Ar = u.FullName_Ar,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                CurrentState = (int)u.CurrentState,
                Roles = pagedRolesByUserId.ContainsKey(u.Id) ? pagedRolesByUserId[u.Id] : new List<string>()
            }).ToList();

            return Json(new DataTableResponse<UserListDto>
            {
                Draw = request.Draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = data
            });
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

            var roles = await _cache.GetOrCreateAsync("all_roles", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _roleManager.Roles
                    .AsNoTracking()
                    .Select(r => new CheckBoxViewModel
                    {
                        RoleId = r.Id.ToString(),
                        DisplayValue = r.Name
                    })
                    .ToListAsync();
            });

            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var addUserVM = new AddUserViewModel
            {
                Roles = roles ?? new List<CheckBoxViewModel>(),
                ConnectionNames = connectionStrings,
                SelectedConnection = selectConn
            };

            return View(addUserVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUser(AddUserViewModel model)
        {
            //if (!ModelState.IsValid)
            //    return View(model);

            if (!model.Roles.Any(r => r.IsSelected))
            {
                ModelState.AddModelError("Roles", "فضلاً، حدد على الأقل مجموعة واحدة");
                return View(model);
            }

            if (await _userManager.FindByNameAsync(model.UserName) != null)
            {
                ModelState.AddModelError("UserName", "اسم المستخدم موجود مسبقاً");
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
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRolesAsync(user,
                model.Roles.Where(r => r.IsSelected).Select(r => r.DisplayValue));

            _cache.Remove("all_roles");

            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "تم بنجاح";
            TempData[Helper.Msg] = "تم إضافة المستخدم بنجاح";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

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
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            user.FullName_Ar = model.FullName_Ar;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;

            await _userManager.UpdateAsync(user);

            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "تم بنجاح";
            TempData[Helper.Msg] = "تم تعديل بيانات المستخدم بنجاح";

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Roles Process [Manage Roles]
        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.AsNoTracking().ToListAsync();
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
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in model.Roles)
            {
                if (userRoles.Contains(role.DisplayValue) && !role.IsSelected)
                    await _userManager.RemoveFromRoleAsync(user, role.DisplayValue);

                if (!userRoles.Contains(role.DisplayValue) && role.IsSelected)
                    await _userManager.AddToRoleAsync(user, role.DisplayValue);
            }

            var updatedRoles = await _userManager.GetRolesAsync(user);
            user.CurrentState = updatedRoles.Count == 0 ? 1 : 2;
            user.EmailConfirmed = updatedRoles.Count > 0;
            await _userManager.UpdateAsync(user);

            TempData[Helper.MsgType] = Helper.Success;
            TempData[Helper.Title] = "تم بنجاح";
            TempData[Helper.Msg] = "تم تحديث صلاحيات المستخدم";

            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int Id)
        {
            var user = await _userManager.FindByIdAsync(Id.ToString());
            if (user == null)
            {
                return Json(new { success = false, message = "المستخدم غير موجود" });
            }

            user.CurrentState = 0;
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);

            return Json(new { success = true, message = "تم تعطيل المستخدم بنجاح" });
        }
        #endregion

        [HttpGet]
        public IActionResult GetUserName(int userId)
        {
            var name = _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.FullName_Ar)
                .FirstOrDefault();

            return Json(new { userName = name });
        }
    }
}