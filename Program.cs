using CRMSystem.BackgroundServices;
using CRMSystem.Configurations;
using CRMSystem.Data;
using CRMSystem.Data.Seeders;
using CRMSystem.Services;
using CRMSystem.Services.GoogleForms;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

//For QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

//For email 
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

//For Google Forms API
builder.Services.Configure<GoogleFormsSettings>(
    builder.Configuration.GetSection("GoogleForms"));

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor for dependency injection
builder.Services.AddHttpContextAccessor();

// Register AuthService for dependency injection
builder.Services.AddScoped<IAuthService, AuthService>();

// Register UserService for dependency injection
builder.Services.AddScoped<IUserService, UserService>();

//Register EmailServices for dependency injection
builder.Services.AddScoped<IEmailService, EmailService>();

// Register NotificationService for dependency injection
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register SLAService for dependency injection
builder.Services.AddScoped<ISLAService, SLAService>();

//Registrer Backgraound SLAService for dependency injection
builder.Services.AddHostedService<SLABackgroundService>();

// Register SalesOfficerPerformanceService for dependency injection
builder.Services.AddScoped< ISalesOfficerPerformanceService, SalesOfficerPerformanceService>();

// Register SalesOfficerService for dependency injection
builder.Services.AddScoped<ISalesOfficerServiceForSalesManager,SalesOfficerServiceForSalesManager>();

// Register PerformanceExportService for dependency injection
builder.Services.AddScoped<IPerformanceExportService, PerformanceExportService>();

// Add Cookie Authentication
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

// Add Authorization
builder.Services.AddAuthorization();


// Add session services
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Make the session cookie HTTP-only
    options.Cookie.IsEssential = true; // Make the session cookie essential
});

// Register LeadService for dependency injection
builder.Services.AddScoped<ILeadService, LeadService>();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Register LeadCaptureService for dependency injection
builder.Services.AddScoped<ILeadCaptureService, LeadCaptureService>();

// Register SettingsService for dependency injection
builder.Services.AddScoped<ISettingsService, SettingsService>();

// Register Sales Manager Dashboard Service
builder.Services.AddScoped<ISalesManagerDashboardService, SalesManagerDashboardService>();

// Register LeadService
builder.Services.AddScoped<ILeadService, LeadService>();

// Register AssignmentService
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

// Register LeadCaptureService
builder.Services.AddScoped<ILeadCaptureService, LeadCaptureService>();

// Register AutoAssignmentService
builder.Services.AddScoped<IAutoAssignmentService, AutoAssignmentService>();

// Register SalesOfficerDashboardService
builder.Services.AddScoped<ISalesOfficerDashboardService, SalesOfficerDashboardService>();

// Register LeadFeedbackService
builder.Services.AddScoped<ILeadFeedbackService, LeadFeedbackService>();

// Register GoogleFormsService
builder.Services.AddScoped<IGoogleFormsService, GoogleFormsService>();

//Register ReportService for dependency injection
builder.Services.AddScoped<IReportService, ReportService>();





var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();