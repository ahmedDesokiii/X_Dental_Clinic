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

var builder = WebApplication.CreateBuilder(args);
//var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContextConnection' not found.");;

//Using Local pc
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
          throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString));

builder.Services.AddAutoMapper(cfg =>{}, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
#region Validation

builder.Services.AddFluentValidationAutoValidation();
#endregion


builder.Services.AddControllersWithViews(); // Adds MVC services\
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();


builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});
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

builder.Services.AddScoped<IClinicalRecordService,ClinicalRecordService>();
#endregion
builder.Services.AddHttpClient();

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
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        await Dental.Seeds.DefaultRoles.SeedAsync(userManager, roleManager);
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
