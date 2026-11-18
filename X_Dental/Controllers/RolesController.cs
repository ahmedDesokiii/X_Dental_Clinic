using Dental.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Dental.Controllers
{
    /// <summary>
    /// 👥 E1 — الأدوار والصلاحيات (مدير النظام فقط).
    /// - دور Admin: كل الصلاحيات تلقائيًا، ما ينفعش يتمسح أو يتغير اسمه أو صلاحياته.
    /// - أدوار النظام (طبيب/استقبال/مستخدم) ما بتتمسحش، والأدوار اللي عليها مستخدمين ما بتتمسحش.
    /// - تعديل الصلاحيات بيشيل claims النوع "Permission" بس (علامة التهيئة SeedVersion بتفضل).
    /// - التعديل بيوصل للمستخدمين فورًا من غير logout: البرنامج بيعيد قراءة الأدوار والصلاحيات
    ///   مع كل طلب (SecurityStampValidatorOptions.ValidationInterval = Zero في Program.cs).
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class RolesController : Controller
    {
        private const string AdminRole = "Admin";

        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public RolesController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        // ── الصفحات ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> ManagePermissions(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return NotFound();
            ViewBag.RoleId = role.Id;
            ViewBag.RoleName = role.Name;
            return View();
        }

        // ── البيانات ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var all = Permissions.GenerateAllPermissions();
            return Json(new { success = true, totalPermissions = all.Count, roles = await LoadRolesAsync(all) });
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissions(int roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null) return Json(new { success = false, message = "الدور غير موجود." });

            var all = Permissions.GenerateAllPermissions();
            var granted = (await _roleManager.GetClaimsAsync(role))
                .Where(c => c.Type == "Permission").Select(c => c.Value).ToHashSet();
            var isAdmin = role.Name == AdminRole;

            var modules = Permissions.ModulesInfo.Select(m => new
            {
                key = m.Key,
                title = m.Title,
                icon = m.Icon,
                description = m.Description,
                permissions = all.Where(p => p.Split('.')[1] == m.Key).Select(p => new
                {
                    value = p,
                    label = p.Split('.', 3)[2],
                    granted = isAdmin || granted.Contains(p)
                })
            });

            var users = await (from ur in _context.UserRoles
                               join u in _context.Users on ur.UserId equals u.Id
                               where ur.RoleId == role.Id
                               orderby u.FullName_Ar
                               select new { u.Id, u.FullName_Ar, u.UserName, u.CurrentState })
                              .ToListAsync();

            return Json(new
            {
                success = true,
                role = new
                {
                    id = role.Id,
                    name = role.Name,
                    displayName = RoleNames.Display(role.Name),
                    details = role.Details,
                    isAdmin,
                    isSystem = RoleNames.IsSystem(role.Name)
                },
                modules,
                presets = new
                {
                    all = all,
                    doctor = all,
                    reception = Permissions.ReceptionDefaults(),
                    readOnly = Permissions.ReadOnlyDefaults(),
                    none = new List<string>()
                },
                users
            });
        }

        // ── إضافة / تعديل دور ──────────────────────────────────
        public class SaveRoleRequest
        {
            public int? RoleId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Details { get; set; }
            public int? CopyFromRoleId { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SaveRole([FromBody] SaveRoleRequest req)
        {
            var name = (req.Name ?? string.Empty).Trim();
            if (name.Length < 2) return Fail("اسم الدور لازم يكون حرفين على الأقل.");

            var existing = await _roleManager.FindByNameAsync(name);

            if (req.RoleId is null or 0)
            {
                if (existing != null) return Fail("فيه دور بنفس الاسم.");
                var role = new ApplicationRole(name) { Details = req.Details?.Trim() ?? string.Empty };
                var r = await _roleManager.CreateAsync(role);
                if (!r.Succeeded) return Fail(string.Join(" — ", r.Errors.Select(e => e.Description)));

                // نسخ صلاحيات دور تاني (اختياري)
                if (req.CopyFromRoleId is > 0)
                {
                    var src = await _roleManager.FindByIdAsync(req.CopyFromRoleId.Value.ToString());
                    if (src != null)
                    {
                        var perms = src.Name == AdminRole
                            ? Permissions.GenerateAllPermissions()
                            : (await _roleManager.GetClaimsAsync(src)).Where(c => c.Type == "Permission").Select(c => c.Value).ToList();
                        foreach (var p in perms.Distinct())
                            await _roleManager.AddClaimAsync(role, new Claim("Permission", p));
                    }
                }
                return Json(new { success = true, message = "تم إنشاء الدور. حدد صلاحياته دلوقتي.", roleId = role.Id });
            }

            var target = await _roleManager.FindByIdAsync(req.RoleId.Value.ToString());
            if (target == null) return Fail("الدور غير موجود.");
            if (target.Name == AdminRole && name != AdminRole) return Fail("ما ينفعش تغيير اسم دور مدير النظام.");
            if (existing != null && existing.Id != target.Id) return Fail("فيه دور تاني بنفس الاسم.");

            target.Name = name;
            target.Details = req.Details?.Trim() ?? string.Empty;
            var u = await _roleManager.UpdateAsync(target);
            if (!u.Succeeded) return Fail(string.Join(" — ", u.Errors.Select(e => e.Description)));

            return Json(new { success = true, message = "تم حفظ الدور." });
        }

        // ── مسح دور ────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteRole(int roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null) return Fail("الدور غير موجود.");
            if (RoleNames.IsSystem(role.Name)) return Fail("ده دور أساسي في النظام وما ينفعش يتمسح.");

            var usersCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id);
            if (usersCount > 0) return Fail($"الدور عليه {usersCount} مستخدم — انقلهم لدور تاني الأول.");

            var r = await _roleManager.DeleteAsync(role);
            return r.Succeeded
                ? Json(new { success = true, message = "تم مسح الدور." })
                : Fail(string.Join(" — ", r.Errors.Select(e => e.Description)));
        }

        // ── حفظ صلاحيات دور ─────────────────────────────────────
        public class SavePermissionsRequest
        {
            public int RoleId { get; set; }
            public List<string> Permissions { get; set; } = new();
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions([FromBody] SavePermissionsRequest req)
        {
            var role = await _roleManager.FindByIdAsync(req.RoleId.ToString());
            if (role == null) return Fail("الدور غير موجود.");
            if (role.Name == AdminRole) return Fail("مدير النظام له كل الصلاحيات تلقائيًا.");

            var valid = Permissions.GenerateAllPermissions().ToHashSet();
            var wanted = (req.Permissions ?? new()).Where(valid.Contains).ToHashSet();

            var current = (await _roleManager.GetClaimsAsync(role)).Where(c => c.Type == "Permission").ToList();

            foreach (var c in current.Where(c => !wanted.Contains(c.Value)))
                await _roleManager.RemoveClaimAsync(role, c);

            var have = current.Select(c => c.Value).ToHashSet();
            foreach (var p in wanted.Where(p => !have.Contains(p)))
                await _roleManager.AddClaimAsync(role, new Claim("Permission", p));

            return Json(new { success = true, message = $"تم حفظ صلاحيات «{RoleNames.Display(role.Name)}» ({wanted.Count} من {valid.Count})." });
        }

        // ── Helpers ────────────────────────────────────────────
        private async Task<object> LoadRolesAsync(List<string> all)
        {
            var roles = await _roleManager.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync();
            var claims = await _context.RoleClaims.AsNoTracking().Where(c => c.ClaimType == "Permission").ToListAsync();
            var counts = await _context.UserRoles.AsNoTracking().GroupBy(ur => ur.RoleId)
                .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count);

            var valid = all.ToHashSet();
            return roles.Select(r =>
            {
                var isAdmin = r.Name == AdminRole;
                var granted = isAdmin ? all : claims.Where(c => c.RoleId == r.Id && valid.Contains(c.ClaimValue!)).Select(c => c.ClaimValue!).Distinct().ToList();
                return new
                {
                    id = r.Id,
                    name = r.Name,
                    displayName = RoleNames.Display(r.Name),
                    details = r.Details,
                    isAdmin,
                    isSystem = RoleNames.IsSystem(r.Name),
                    usersCount = counts.GetValueOrDefault(r.Id),
                    permissionsCount = granted.Count,
                    permissions = granted,
                    modules = Permissions.ModulesInfo
                        .Where(m => granted.Any(p => p.Split('.')[1] == m.Key))
                        .Select(m => new { m.Title, m.Icon })
                };
            }).ToList();
        }

        private JsonResult Fail(string message) => Json(new { success = false, message });
    }
}
