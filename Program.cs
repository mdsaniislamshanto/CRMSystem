using CRMSystem.Data;
using CRMSystem.Data.Seeders;
using CRMSystem.Services;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using CRMSystem.Configurations;


var builder = WebApplication.CreateBuilder(args);

//For email 
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();



app.Run();