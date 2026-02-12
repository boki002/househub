using househub.Data;
using househub.Models;
using househub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Betoltjuk a connection stringet az appsettings.json-bol.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A 'DefaultConnection' connection string hianyzik az appsettings.json-bol.");

// Itt allitjuk be az EF Core-t, hogy MariaDB-t hasznaljon.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Fix szerververzio: ne probaljon mar indulaskor kapcsolodni AutoDetect miatt.
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(10, 11, 0)));
});

// SMTP email kuldes beallitasa (Identity email-ekhez).
builder.Services.Configure<SmtpEmailSettings>(
    builder.Configuration.GetSection(SmtpEmailSettings.SectionName));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// Identity beallitasa a sajat ApplicationUser osztallyal.
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Jelszokovetelmenyek (fejlesztoi egyszerusites).
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;

    // Regisztracio utan kotelezo email megerosites.
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// MVC + Razor Pages.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// Adatbazis letrehozasa es kezdo adatok feltoltese indulasnal.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.EnsureCreated();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        DbSeeder.SeedAsync(roleManager, userManager).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Hiba tortent az adatbazis inicializalasa kozben.");
    }
}

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
