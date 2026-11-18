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

        #region Index
        // 👥 E1: الصفحة بتجيب بياناتها من GetAll (JSON)
        [HttpGet]
        public IActionResult Index() => View();
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
                PassUser = string.Empty,   // 🔒 E1: ما بنحفظش كلمة السر كنص مكشوف
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

            // 🔒 E1: نفس حماية SetState
            if (user.Id == CurrentUserId)
                return Json(new { success = false, message = "ما ينفعش تعطل حسابك وانت داخل بيه." });
            if (await _userManager.IsInRoleAsync(user, AdminRole) && await OtherActiveAdminsAsync(user.Id) == 0)
                return Json(new { success = false, message = "ده آخر مدير نظام نشط — ما ينفعش يتعطل." });

            user.CurrentState = 0;
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            return Json(new { success = true, message = "تم تعطيل المستخدم بنجاح" });
        }
        #endregion


        // ════════════════════════════════════════════════════════════
        // 👥 E1 — واجهة المستخدمين الجديدة (JSON)
        // ════════════════════════════════════════════════════════════

        private const string AdminRole = "Admin";

        private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        private async Task<int> OtherActiveAdminsAsync(int exceptUserId)
        {
            var admins = await _userManager.GetUsersInRoleAsync(AdminRole);
            return admins.Count(a => a.Id != exceptUserId && a.CurrentState == 2);
        }

        private static string Arabic(IdentityError e) => e.Code switch
        {
            "PasswordTooShort" => "كلمة السر قصيرة (6 حروف على الأقل).",
            "PasswordRequiresDigit" => "كلمة السر لازم فيها رقم.",
            "PasswordRequiresLower" => "كلمة السر لازم فيها حرف إنجليزي صغير (a-z).",
            "PasswordRequiresUpper" => "كلمة السر لازم فيها حرف إنجليزي كبير (A-Z).",
            "PasswordRequiresNonAlphanumeric" => "كلمة السر لازم فيها رمز (مثل @ # $).",
            "DuplicateUserName" => "اسم الدخول مستخدم قبل كده.",
            "InvalidUserName" => "اسم الدخول لازم يكون حروف إنجليزي أو أرقام من غير مسافات.",
            "DuplicateEmail" => "البريد مستخدم قبل كده.",
            "InvalidEmail" => "البريد غير صحيح.",
            _ => e.Description
        };

        private JsonResult Fail(string message) => Json(new { success = false, message });
        private JsonResult Fail(IdentityResult r) => Fail(string.Join("\n", r.Errors.Select(Arabic)));

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _context.Users.AsNoTracking().OrderBy(u => u.FullName_Ar).ToListAsync();
            var map = await _context.UserRoles.AsNoTracking().ToListAsync();
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
            var roleById = roles.ToDictionary(r => r.Id, r => r.Name ?? "");

            var all = Permissions.GenerateAllPermissions();
            var valid = all.ToHashSet();
            var claims = await _context.RoleClaims.AsNoTracking().Where(c => c.ClaimType == "Permission").ToListAsync();
            var now = DateTimeOffset.UtcNow;
            var me = CurrentUserId;

            return Json(new
            {
                success = true,
                me,
                totalPermissions = all.Count,
                allPermissions = all,
                modules = Permissions.ModulesInfo.Select(m => new { m.Key, m.Title, m.Icon }),
                roles = roles.Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    displayName = RoleNames.Display(r.Name),
                    details = r.Details,
                    isAdmin = r.Name == AdminRole,
                    permissions = r.Name == AdminRole
                        ? all
                        : claims.Where(c => c.RoleId == r.Id && valid.Contains(c.ClaimValue!)).Select(c => c.ClaimValue!).Distinct().ToList()
                }),
                users = users.Select(u => new
                {
                    id = u.Id,
                    code = u.Code,
                    fullName = u.FullName_Ar,
                    userName = u.UserName,
                    email = u.Email,
                    phone = u.PhoneNumber,
                    gender = u.Gender,
                    state = u.CurrentState ?? 0,
                    lockedOut = u.LockoutEnd.HasValue && u.LockoutEnd > now,
                    isMe = u.Id == me,
                    roles = map.Where(m => m.UserId == u.Id && roleById.ContainsKey(m.RoleId)).Select(m => roleById[m.RoleId]).ToList()
                })
            });
        }

        public class SaveUserRequest
        {
            public int? Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Code { get; set; }
            public string? Gender { get; set; }
            public string? Password { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser([FromBody] SaveUserRequest req)
        {
            req.FullName = (req.FullName ?? "").Trim();
            req.UserName = (req.UserName ?? "").Trim();
            if (req.FullName.Length < 3) return Fail("اكتب اسم المستخدم بالكامل.");
            if (req.UserName.Length < 3) return Fail("اسم الدخول لازم 3 حروف على الأقل.");

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            var wantedRoles = (req.Roles ?? new()).Where(allRoles.Contains).Distinct().ToList();
            if (wantedRoles.Count == 0) return Fail("اختار دور واحد على الأقل.");

            // ── إضافة ──
            if (req.Id is null or 0)
            {
                if (string.IsNullOrWhiteSpace(req.Password)) return Fail("اكتب كلمة السر.");

                var code = string.IsNullOrWhiteSpace(req.Code)
                    ? "U" + ((await _context.Users.CountAsync()) + 1).ToString("D3")
                    : req.Code.Trim();

                var user = new ApplicationUser
                {
                    Code = code,
                    FullName_Ar = req.FullName,
                    UserName = req.UserName,
                    Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim(),
                    PhoneNumber = req.Phone?.Trim(),
                    Gender = req.Gender ?? string.Empty,
                    EmailConfirmed = true,
                    CurrentState = 2,
                    PassUser = string.Empty
                };
                var r = await _userManager.CreateAsync(user, req.Password);
                if (!r.Succeeded) return Fail(r);
                await _userManager.AddToRolesAsync(user, wantedRoles);
                _cache.Remove("all_roles");
                return Json(new { success = true, message = $"تم إضافة «{user.FullName_Ar}»." });
            }

            // ── تعديل ──
            var target = await _userManager.FindByIdAsync(req.Id.Value.ToString());
            if (target == null) return Fail("المستخدم غير موجود.");

            var currentRoles = await _userManager.GetRolesAsync(target);
            var losingAdmin = currentRoles.Contains(AdminRole) && !wantedRoles.Contains(AdminRole);
            if (losingAdmin && target.Id == CurrentUserId)
                return Fail("ما ينفعش تشيل صلاحية مدير النظام من نفسك.");
            if (losingAdmin && await OtherActiveAdminsAsync(target.Id) == 0)
                return Fail("ده آخر مدير نظام نشط — لازم يفضل مدير واحد على الأقل.");

            if (!string.Equals(target.UserName, req.UserName, StringComparison.Ordinal))
            {
                var rn = await _userManager.SetUserNameAsync(target, req.UserName);
                if (!rn.Succeeded) return Fail(rn);
            }

            target.FullName_Ar = req.FullName;
            target.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            target.NormalizedEmail = target.Email?.ToUpperInvariant();
            target.PhoneNumber = req.Phone?.Trim();
            target.Gender = req.Gender ?? target.Gender;
            if (!string.IsNullOrWhiteSpace(req.Code)) target.Code = req.Code.Trim();
            var up = await _userManager.UpdateAsync(target);
            if (!up.Succeeded) return Fail(up);

            var toRemove = currentRoles.Except(wantedRoles).ToList();
            var toAdd = wantedRoles.Except(currentRoles).ToList();
            if (toRemove.Count > 0) await _userManager.RemoveFromRolesAsync(target, toRemove);
            if (toAdd.Count > 0) await _userManager.AddToRolesAsync(target, toAdd);

            if (!string.IsNullOrWhiteSpace(req.Password))
            {
                var pr = await ResetPasswordInternalAsync(target, req.Password);
                if (!pr.Succeeded) return Fail(pr);
            }

            // تعديل نفسي → نجدد الكوكي عشان ما يحصلش logout
            if (target.Id == CurrentUserId) await _signInManager.RefreshSignInAsync(target);

            return Json(new { success = true, message = $"تم حفظ «{target.FullName_Ar}»." });
        }

        public class ResetPasswordRequest
        {
            public int Id { get; set; }
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
        {
            var user = await _userManager.FindByIdAsync(req.Id.ToString());
            if (user == null) return Fail("المستخدم غير موجود.");
            if (string.IsNullOrWhiteSpace(req.Password)) return Fail("اكتب كلمة السر الجديدة.");

            var r = await ResetPasswordInternalAsync(user, req.Password);
            if (!r.Succeeded) return Fail(r);

            if (user.Id == CurrentUserId) await _signInManager.RefreshSignInAsync(user);
            return Json(new { success = true, message = "تم تغيير كلمة السر. أي جهاز تاني كان داخل بالحساب ده هيطلب تسجيل دخول." });
        }

        private async Task<IdentityResult> ResetPasswordInternalAsync(ApplicationUser user, string password)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var r = await _userManager.ResetPasswordAsync(user, token, password);   // بيحدّث SecurityStamp تلقائيًا
            if (r.Succeeded)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }
            return r;
        }

        public class SetStateRequest
        {
            public int Id { get; set; }
            /// <summary>2 = نشط، 1 = معلّق، 0 = معطّل</summary>
            public int State { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SetState([FromBody] SetStateRequest req)
        {
            if (req.State is < 0 or > 2) return Fail("حالة غير صحيحة.");
            var user = await _userManager.FindByIdAsync(req.Id.ToString());
            if (user == null) return Fail("المستخدم غير موجود.");

            if (req.State != 2)
            {
                if (user.Id == CurrentUserId) return Fail("ما ينفعش توقف حسابك وانت داخل بيه.");
                if (await _userManager.IsInRoleAsync(user, AdminRole) && await OtherActiveAdminsAsync(user.Id) == 0)
                    return Fail("ده آخر مدير نظام نشط — ما ينفعش يتوقف.");
            }

            user.CurrentState = req.State;
            user.EmailConfirmed = req.State != 0;   // المعطّل ما يقدرش يسجل دخول (RequireConfirmedAccount)
            await _userManager.UpdateAsync(user);

            if (req.State != 2)
                await _userManager.UpdateSecurityStampAsync(user);   // يطلع من أي جهاز فورًا
            else
                await _userManager.SetLockoutEndDateAsync(user, null);

            var msg = req.State switch
            {
                2 => $"تم تفعيل «{user.FullName_Ar}».",
                1 => $"تم تعليق «{user.FullName_Ar}» — اتعمله خروج من كل الأجهزة.",
                _ => $"تم تعطيل «{user.FullName_Ar}» — اتعمله خروج من كل الأجهزة."
            };
            return Json(new { success = true, message = msg });
        }

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