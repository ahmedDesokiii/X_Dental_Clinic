using Dental.Data;
using Dental.Modules.Appointments.InterfacesAppointments;
using Dental.Modules.Appointments.ServicesAppointments;
using Dental.Modules.Clinical.Interfaces;
using Dental.Modules.Clinical.Services;
using Dental.Modules.Diagnosis.InterfacesDiagnosis;
using Dental.Modules.Diagnosis.ServicesDiagnosis;
using Dental.Modules.Inventory.InterfacesInventory;
using Dental.Modules.Inventory.ServicesInventory;
using Dental.Modules.Laboratory.InterfacesLaboratory;
using Dental.Modules.Laboratory.ServicesLaboratory;
using Dental.Modules.Patients.InterfacesPatients;
using Dental.Modules.Patients.ServicesPatients;
using Dental.Modules.Payments.InterfacesPayments;
using Dental.Modules.Payments.ServicesPayments;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// 🛡️ A5: تسجيل الأخطاء في ملفات يومية (Logs/dental-YYYYMMDD.log) — بنحتفظ بآخر 30 يوم
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(ctx.HostingEnvironment.ContentRootPath, "Logs", "dental-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"));
//var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");;

//Using Local pc
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// 🔒 A4: سجل التعديلات + CreatedBy/ModifiedBy أوتوماتيك
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Dental.Data.Interceptors.AuditSaveChangesInterceptor>();
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<Dental.Data.Interceptors.AuditSaveChangesInterceptor>()));

builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
#region Validation

builder.Services.AddFluentValidationAutoValidation();
#endregion


// 🔒 A1: كل POST/PUT/DELETE لازم يحمل Antiforgery token (الـ JS بيضيفه أوتوماتيك — wwwroot/js/antiforgery.js)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new ModulePermissionFilter());   // 🔒 A3: صلاحيات كل موديول
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

// 🔒 A1: أي صفحة/endpoint مقفول افتراضيًا إلا لو عليه [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();


builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

// (الـ logging بقى من Serilog فوق)

// 🛡️ A5: نسخة احتياطية يومية للداتابيز (الإعدادات في appsettings → "Backup")
builder.Services.AddSingleton<Dental.Core.Maintenance.DatabaseBackupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Dental.Core.Maintenance.DatabaseBackupService>());
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    serverOptions.Limits.MaxRequestHeadersTotalSize = 32768;
    serverOptions.Limits.MaxRequestBodySize = 104857600;
});

// Add services to the container.
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(
    options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultUI()
                .AddDefaultTokenProviders();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//Services To ReloadPage after update any process 
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources"); // Specify resource file location
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(); // For localizing validation messages

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
            new CultureInfo("en-US"),
            new CultureInfo("ar") // Add Arabic culture
        };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider()); // Use cookie to store culture
});

#region Scoped Services
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IDiagnosisService, DiagnosisService>();
builder.Services.AddScoped<ILaboratoryService, LaboratoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddScoped<IClinicalRecordService, ClinicalRecordService>();
builder.Services.AddScoped<IOrthoService, OrthoService>();
builder.Services.AddScoped<IVisitNoteService, VisitNoteService>();   // ⭐ المرحلة 2 — ملاحظات المتابعة
#endregion
builder.Services.AddHttpClient();

// ⚙️ D1: إعدادات العيادة (App_Data/clinic-settings.json)
builder.Services.AddSingleton<Dental.Core.Settings.IClinicSettingsService, Dental.Core.Settings.ClinicSettingsService>();

builder.Services.AddMemoryCache();

var app = builder.Build();
app.UseSession();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("app");
    try
    {
        // تنبيه لو فيه migrations لسه ما اتطبقتش على الداتابيز (Update-Database)
        var db = services.GetRequiredService<ApplicationDbContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
            logger.LogWarning("Pending EF migrations not applied: {Migrations}", string.Join(", ", pending));

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await Dental.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
        await Dental.Seeds.DefaultRoles.SeedPermissionsAsync(roleManager);   // 🔒 A3
        await Dental.Seeds.DefaultUsers.SeedUserAsync(userManager, roleManager);
        await Dental.Seeds.DefaultUsers.SeedAdminAsync(userManager, roleManager);

        logger.LogInformation("Finished Seeding Default Data");
        logger.LogInformation("Application Starting");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "An error occurred seeding the DB");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();

// 🔒 A2: ملفات الأشعة والمستندات (/uploads) ما تتفتحش غير لمستخدم عامل login
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/uploads"), uploads =>
{
    uploads.UseAuthentication();
    uploads.Use(async (ctx, next) =>
    {
        if (ctx.User?.Identity?.IsAuthenticated != true)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        ctx.Response.Headers["Cache-Control"] = "private, no-store";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next();
    });
});

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
//app.MapHub<ProgressHub>("/progressHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
