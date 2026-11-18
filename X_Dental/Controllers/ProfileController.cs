using Dental.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Dental.Controllers
{
    /// <summary>
    /// 🙍 H4 — بروفايل المستخدم الحالي (أي مستخدم داخل).
    /// بياناتي، صورتي، كلمة السر، الخروج من الأجهزة التانية، صلاحياتي، ونشاطي.
    /// صورة المستخدم: wwwroot/uploads/avatars/{id}.(jpg|png|webp) — محمية بالـ login (A2).
    /// </summary>
    public class ProfileController : Controller
    {
        private static readonly string[] AvatarExt = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IAuthorizationService _auth;

        public ProfileController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn,
            ApplicationDbContext context, IWebHostEnvironment env, IAuthorizationService auth)
        {
            _users = users;
            _signIn = signIn;
            _context = context;
            _env = env;
            _auth = auth;
        }

        public static string? AvatarUrl(IWebHostEnvironment env, int userId)
        {
            var dir = Path.Combine(env.WebRootPath, "uploads", "avatars");
            foreach (var ext in AvatarExt)
            {
                var f = Path.Combine(dir, $"{userId}{ext}");
                if (System.IO.File.Exists(f))
                    return $"/uploads/avatars/{userId}{ext}?v={System.IO.File.GetLastWriteTimeUtc(f).Ticks}";
            }
            return null;
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetData()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Json(new { success = false });

            var roles = await _users.GetRolesAsync(user);
            var isAdmin = roles.Contains("Admin");

            // صلاحياتي الفعلية لكل موديول
            var all = Permissions.GenerateAllPermissions();
            var granted = new List<string>();
            foreach (var p in all)
                if ((await _auth.AuthorizeAsync(User, p)).Succeeded) granted.Add(p);

            var modules = Permissions.ModulesInfo.Select(m =>
            {
                var mine = all.Where(p => p.Split('.')[1] == m.Key).ToList();
                var on = mine.Where(granted.Contains).Select(p => p.Split('.', 3)[2]).ToList();
                return new { m.Title, m.Icon, total = mine.Count, granted = on };
            });

            // نشاطي
            var since = DateTime.Today.AddDays(-30);
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var canMoney = (await _auth.AuthorizeAsync(User, Permissions.Payments.View)).Succeeded;

            var stats = new
            {
                patients = await _context.Patients.CountAsync(p => p.CreatedBy == user.Id && p.CreatedDate >= monthStart.ToUniversalTime()),
                visits = await _context.Visits.CountAsync(v => v.CreatedBy == user.Id && v.CreatedDate >= monthStart.ToUniversalTime()),
                appointments = await _context.Appointments.CountAsync(a => a.CreatedBy == user.Id && a.CreatedDate >= monthStart.ToUniversalTime()),
                payments = await _context.VisitPayments.CountAsync(p => p.CreatedBy == user.Id && !p.IsDeleted && p.PaymentDate >= monthStart),
                paymentsTotal = canMoney
                    ? await _context.VisitPayments.Where(p => p.CreatedBy == user.Id && !p.IsDeleted && p.PaymentDate >= monthStart).SumAsync(p => (decimal?)p.Amount) ?? 0
                    : (decimal?)null
            };

            var activity = await _context.AuditLogs.AsNoTracking()
                .Where(a => a.UserId == user.Id && a.Timestamp >= since.ToUniversalTime())
                .OrderByDescending(a => a.Id)
                .Take(25)
                .Select(a => new { a.Timestamp, a.EntityName, a.EntityId, a.Action })
                .ToListAsync();

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName_Ar,
                    userName = user.UserName,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    gender = user.Gender,
                    code = user.Code,
                    avatar = AvatarUrl(_env, user.Id) is string av ? Url.Content("~" + av) : null,
                    roles = roles.Select(r => new { name = r, displayName = RoleNames.Display(r) }),
                    isAdmin
                },
                totalPermissions = all.Count,
                grantedCount = granted.Count,
                modules,
                stats,
                activity = activity.Select(a => new
                {
                    at = DateTime.SpecifyKind(a.Timestamp, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-ddTHH:mm:ss"),
                    what = EntityLabel(a.EntityName),
                    a.EntityId,
                    action = a.Action switch
                    {
                        "Modified" => "عدّل",
                        "Deleted" => "مسح",
                        "SoftDeleted" => "ألغى",
                        "Restored" => "استرجع",
                        _ => a.Action
                    },
                    tone = a.Action
                })
            });
        }

        public class SaveProfileRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Gender { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveProfileRequest req)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Fail("المستخدم غير موجود.");
            if ((req.FullName ?? "").Trim().Length < 3) return Fail("اكتب اسمك بالكامل.");

            user.FullName_Ar = req.FullName!.Trim();
            user.Email = string.IsNullOrWhiteSpace(req.Email) ? null : req.Email.Trim();
            user.PhoneNumber = req.Phone?.Trim();
            if (req.Gender != null) user.Gender = req.Gender;

            var r = await _users.UpdateAsync(user);
            if (!r.Succeeded) return Fail(string.Join("\n", r.Errors.Select(e => e.Description)));
            await _signIn.RefreshSignInAsync(user);
            return Json(new { success = true, message = "تم حفظ بياناتك." });
        }

        public class ChangePasswordRequest
        {
            public string Current { get; set; } = string.Empty;
            public string New { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Fail("المستخدم غير موجود.");
            if (string.IsNullOrWhiteSpace(req.Current) || string.IsNullOrWhiteSpace(req.New)) return Fail("اكتب كلمة السر الحالية والجديدة.");

            var r = await _users.ChangePasswordAsync(user, req.Current, req.New);
            if (!r.Succeeded)
            {
                var msg = string.Join("\n", r.Errors.Select(e => e.Code switch
                {
                    "PasswordMismatch" => "كلمة السر الحالية غلط.",
                    "PasswordTooShort" => "كلمة السر قصيرة (6 حروف على الأقل).",
                    "PasswordRequiresDigit" => "لازم فيها رقم.",
                    "PasswordRequiresLower" => "لازم فيها حرف إنجليزي صغير.",
                    "PasswordRequiresUpper" => "لازم فيها حرف إنجليزي كبير.",
                    "PasswordRequiresNonAlphanumeric" => "لازم فيها رمز (مثل @ # $).",
                    _ => e.Description
                }));
                return Fail(msg);
            }
            await _signIn.RefreshSignInAsync(user);   // يفضل داخل على الجهاز ده، والباقي يطلع
            return Json(new { success = true, message = "تم تغيير كلمة السر. أي جهاز تاني هيطلب تسجيل دخول." });
        }

        [HttpPost]
        public async Task<IActionResult> SignOutOthers()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Fail("المستخدم غير موجود.");
            await _users.UpdateSecurityStampAsync(user);
            await _signIn.RefreshSignInAsync(user);
            return Json(new { success = true, message = "تم تسجيل الخروج من كل الأجهزة التانية." });
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile? file, bool remove = false)
        {
            var user = await _users.GetUserAsync(User);
            if (user == null) return Fail("المستخدم غير موجود.");

            var dir = Path.Combine(_env.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(dir);
            foreach (var ext in AvatarExt)
            {
                var old = Path.Combine(dir, $"{user.Id}{ext}");
                if (System.IO.File.Exists(old)) System.IO.File.Delete(old);
            }
            if (remove) return Json(new { success = true, message = "تم مسح الصورة.", avatar = (string?)null });

            if (file == null || file.Length == 0) return Fail("اختار صورة.");
            if (file.Length > 3 * 1024 * 1024) return Fail("الصورة أكبر من 3 ميجا.");
            var e = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AvatarExt.Contains(e)) return Fail("الصورة لازم تكون JPG أو PNG أو WEBP.");

            var path = Path.Combine(dir, $"{user.Id}{e}");
            await using (var fs = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(fs);

            var url = AvatarUrl(_env, user.Id);
            return Json(new { success = true, message = "تم تحديث صورتك.", avatar = url == null ? null : Url.Content("~" + url) });
        }

        private static string EntityLabel(string name) => name switch
        {
            "Patient" => "ملف مريض",
            "PatientMedicalInfo" => "بيانات طبية",
            "PatientContact" => "بيانات تواصل",
            "PatientDocument" => "مستند",
            "PatientXRay" => "أشعة",
            "Visit" => "زيارة",
            "VisitDiagnosis" => "تشخيص",
            "VisitProcedure" => "إجراء",
            "VisitPayment" => "دفعة",
            "Appointment" => "موعد",
            "Prescription" => "روشتة",
            "PrescriptionItem" => "دوا في روشتة",
            "TreatmentPlan" => "خطة علاج",
            "TreatmentPlanStep" => "خطوة في خطة علاج",
            "OrthoCase" => "حالة تقويم",
            "OrthoVisitEntry" => "جلسة تقويم",
            "DiagnosisCatalog" => "تشخيص في القائمة",
            "MedicationCatalog" => "دوا في القائمة",
            "Material" => "صنف مخزن",
            "DentalChartEntry" => "سجل سنة",
            _ => name
        };

        private JsonResult Fail(string message) => Json(new { success = false, message });
    }
}
