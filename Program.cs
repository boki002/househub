using househub.Data;
using househub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

// Betöltjük a connection stringet az appsettings.json-ból
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("A 'DefaultConnection' connection string hiányzik az appsettings.json-ból.");

// Itt állítjuk be az EF Core-t, hogy MariaDB-t használjon
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Fontos: Pomelo MariaDB provider konfigurálása
    // ServerVersion.AutoDetect -> automatikusan felismeri a MariaDB verzióját
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Identity beállítása a saját ApplicationUser osztályunkkal
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Jelszókövetelmények (egyszerűsítve, hogy fejlesztéshez ne legyen túl szigorú)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
    .AddRoles<IdentityRole>() // Szerepkörök támogatása (admin, stb.)
    .AddEntityFrameworkStores<ApplicationDbContext>(); // Identity adatok is MariaDB-ben lesznek

// MVC támogatás (Controller + View)
builder.Services.AddControllersWithViews();
// Identity UI (Login/Register) Razor Pages endpointjeihez szksges
builder.Services.AddRazorPages();
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// ADATBÁZIS LÉTREHOZÁSA ÉS KEZDŐ ADATOK FELTÖLTÉSE ALKALMAZÁS INDULÁSKOR
using (var scope = app.Services.CreateScope())
{
    // A scope-ból ki tudjuk kérni a szükséges szolgáltatásokat
    var services = scope.ServiceProvider;

    try
    {
        // 1) Lekérjük az adatbázis kontextust (ApplicationDbContext)
        var dbContext = services.GetRequiredService<ApplicationDbContext>();

        // ------------------------------
        // EnsureCreated:
        // ------------------------------
        // Ez a metódus ellenőrzi, hogy léteznek-e az adatbázis objektumok (adatbázis + táblák).
        // Ha még nem léteznek, automatikusan létrehozza őket EF Core alapján.
        // FONTOS: Ez NEM migráció, hanem egy egyszerű "ha nincs, hozd létre" mechanizmus.
        // Nekünk most ez pont elég, és megkerüljük vele az EF Tools hibáját.
        dbContext.Database.EnsureCreated();

        // 2) Lekérjük a szerepkör- és felhasználókezelő szolgáltatásokat Identity-hez
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // 3) Lefuttatjuk az általunk írt adatbázis seeder metódust,
        //    ami létrehozza a szerepköröket (Admin, Ingatlanos, Tulajdonos)
        //    és egy alap admin felhasználót (admin@househub.local / Admin123)
        DbSeeder.SeedAsync(roleManager, userManager).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        // Ha itt hiba történik, naplózzuk, hogy könnyebb legyen a hibakeresés.
        app.Logger.LogError(ex, "Hiba történt az adatbázis inicializálása közben.");
    }
}

// Fejlesztési és éles környezet beállítások
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTPS kényszerítés
app.UseHttpsRedirection();

// Statikus fájlok (pl. CSS, JS, képek)
app.UseStaticFiles();

// Routing middleware
app.UseRouting();

// Authentication & Authorization middleware-ek
app.UseAuthentication();
app.UseAuthorization();

// Alap route beállítása (HomeController / Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Identity area (Register, Login, stb.)
app.MapRazorPages();

app.Run();
