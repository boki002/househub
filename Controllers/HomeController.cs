using System.Diagnostics;
using househub.Models;                // ErrorViewModel, Property, stb. (ha nálad househub_P.Models, akkor azt írd ide)
using househub.Data;                  // ApplicationDbContext eléréséhez
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ToListAsync, Include, OrderByDescending

namespace househub_P.Controllers
{
    public class HomeController : Controller
    {
        // Logger – marad, ha késõbb logolni szeretnél
        private readonly ILogger<HomeController> _logger;

        // Az EF Core adatbázis kontextus, amin keresztül elérjük a Property táblát
        private readonly ApplicationDbContext _context;

        // Konstruktor – a DI (dependency injection) adja be az ILogger és az ApplicationDbContext példányt
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // ----------------------------------------------------
        // FÕOLDAL (Index) – legfrissebb ingatlan hirdetések
        // ----------------------------------------------------

        // GET: /Home/Index
        public async Task<IActionResult> Index()
        {
            // Lekérdezzük a legutóbbi 6 ingatlant.
            // Include(p => p.Kepek) -> hogy legyenek betöltve a képek is (pl. az elsõ kép thumbnailnek).
            var latestProperties = await _context.Properties
                .Include(p => p.Kepek)
                .OrderByDescending(p => p.Letrehozva)
                .Take(6)
                .ToListAsync();

            // A listát átadjuk a nézetnek modellként
            // A Views/Home/Index.cshtml-ben @model IEnumerable<Property> lesz ehhez.
            return View(latestProperties);
        }

        // Adatvédelmi oldal (sablon)
        public IActionResult Privacy()
        {
            return View();
        }

        // Hibakezelõ akció – az Error.cshtml nézetet használja ErrorViewModel modellel
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
