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

// For QuestPDF License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Configure Email Settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure Google Forms API Settings
builder.Services.Configure<GoogleFormsSettings>(
    builder.Configuration.GetSection("GoogleForms"));

// Add MVC services
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor
builder.Services.AddHttpContextAccessor();


// =====================================================
// Authentication & Authorization
// =====================================================

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();


// =====================================================
// Session
// =====================================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


// =====================================================
// Database
// =====================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection"))
    ));


// =====================================================
// Core Services
// =====================================================

// Authentication
builder.Services.AddScoped<IAuthService, AuthService>();

// Users
builder.Services.AddScoped<IUserService, UserService>();

// Email
builder.Services.AddScoped<IEmailService, EmailService>();

// Notifications
builder.Services.AddScoped<INotificationService, NotificationService>();

// SLA
builder.Services.AddScoped<ISLAService, SLAService>();

// Lead
builder.Services.AddScoped<ILeadService, LeadService>();

// Lead Capture
builder.Services.AddScoped<ILeadCaptureService, LeadCaptureService>();

// Assignment
builder.Services.AddScoped<IAssignmentService, AssignmentService>();

// Auto Assignment
builder.Services.AddScoped<IAutoAssignmentService, AutoAssignmentService>();

// Lead Feedback
builder.Services.AddScoped<ILeadFeedbackService, LeadFeedbackService>();

// Follow Up
builder.Services.AddScoped<IFollowUpService, FollowUpService>();

// Settings
builder.Services.AddScoped<ISettingsService, SettingsService>();


// =====================================================
// Sales Manager Services
// =====================================================

// Sales Manager Dashboard
builder.Services.AddScoped<
    ISalesManagerDashboardService,
    SalesManagerDashboardService>();

// Sales Officer Management
builder.Services.AddScoped<
    ISalesOfficerServiceForSalesManager,
    SalesOfficerServiceForSalesManager>();

// Sales Officer Performance
builder.Services.AddScoped<
    ISalesOfficerPerformanceService,
    SalesOfficerPerformanceService>();

// Performance Export
builder.Services.AddScoped<
    IPerformanceExportService,
    PerformanceExportService>();


// =====================================================
// Sales Officer Dashboard
// =====================================================

builder.Services.AddScoped<
    ISalesOfficerDashboardService,
    SalesOfficerDashboardService>();


// =====================================================
// Reports
// =====================================================

builder.Services.AddScoped<
    IReportService,
    ReportService>();


// =====================================================
// Google Forms
// =====================================================

builder.Services.AddScoped<
    IGoogleFormsService,
    GoogleFormsService>();


// =====================================================
// Background Services
// =====================================================

// SLA monitoring
builder.Services.AddHostedService<SLABackgroundService>();

// Automatic Google Form Lead Import
builder.Services.AddHostedService<GoogleLeadPollingService>();


var app = builder.Build();


// =====================================================
// Database Migration & Seeder
// =====================================================

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();

    await DatabaseSeeder.SeedAsync(context);
}


// =====================================================
// HTTP Request Pipeline
// =====================================================

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