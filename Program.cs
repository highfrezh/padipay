using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PadiPay.Data;
using PadiPay.Models;
using PadiPay.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<INotificationService, NotificationService>();


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Allows users to sign in immediately after registering without having to click an email confirmation link first (ideal for smooth development & testing).
    options.SignIn.RequireConfirmedAccount = false;

    // Disables the requirement for passwords to contain uppercase letters or special characters
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    
    // Sets the minimum password length to 6 characters (instead of the default 8)
    options.Password.RequiredLength = 6;
})
// Configures Entity Framework Core to use the ApplicationDbContext for Identity operations (user storage, role management, etc.)
.AddEntityFrameworkStores<ApplicationDbContext>()
// Adds support for generating and validating various ASP.NET Core Identity tokens (e.g., for email confirmation, password reset, two-factor authentication)
.AddDefaultTokenProviders()
// Integrates ASP.NET Core Identity with the default UI pages provided by the Identity framework
.AddDefaultUI();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// Automatically apply pending EF Core migrations & seed database on startup
using (var scope = app.Services.CreateScope())

{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DbInitializer.SeedAsync(services);
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
