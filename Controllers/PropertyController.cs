using Homecat.Data;               // ApplicationDbContext eléréséhez
using Homecat.Models;             // Property, PropertyCategory, stb.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;                    // Fájlműveletekhez (Path, FileStream, Directory)
using Microsoft.AspNetCore.Http;    // IFormFile
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment



namespace Homecat.Controllers
{
    // Ez a controller kezeli az ingatlan hirdetéseket (Property)
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // A webalkalmazás környezete, ebből tudjuk meg a wwwroot fizikai elérési útját
        private readonly IWebHostEnvironment _env;

        // Maximálisan engedélyezett képek száma egy ingatlanhoz
        private const int MaxImagesPerProperty = 10;

        // Konstruktor – most már az IWebHostEnvironment-et is bekérjük DI-ből
        public PropertyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

   


        // -------------------------------------------------------
        // LISTA + EGYSZERŰ KERESÉS
        // -------------------------------------------------------

        // GET: /Property
        // Ez az akció listázza az ingatlan hirdetéseket, és
        // opcionálisan egyszerű keresést végez:
        // - keresőszó (kulcsszó) címben/leírásban
        // - kategória (lakás/ház/telek/iroda)
        // - minimum és maximum ár
        public async Task<IActionResult> Index(
            string? keresoszo,
            PropertyCategory? kategoria,
            int? minAr,
            int? maxAr)
        {
            // Alap lekérdezés: minden ingatlan
            var query = _context.Properties
                .Include(p => p.TulajdonosUser) // betöltjük a tulajdonos felhasználót is
                .AsQueryable();

            // Ha megadtak keresőszót, akkor címre és leírásra szűrünk
            if (!string.IsNullOrWhiteSpace(keresoszo))
            {
                string k = keresoszo.Trim().ToLower();

                query = query.Where(p =>
                    p.Cim.ToLower().Contains(k) ||
                    (p.Leiras != null && p.Leiras.ToLower().Contains(k)));
            }

            // Kategória szerinti szűrés (ha meg van adva)
            if (kategoria.HasValue)
            {
                query = query.Where(p => p.Category == kategoria.Value);
            }

            // Minimum ár (ha meg van adva)
            if (minAr.HasValue)
            {
                query = query.Where(p => p.Ar >= minAr.Value);
            }

            // Maximum ár (ha meg van adva)
            if (maxAr.HasValue)
            {
                query = query.Where(p => p.Ar <= maxAr.Value);
            }

            // Rendezés: legújabb elöl
            query = query.OrderByDescending(p => p.Letrehozva);

            // Lekérdezés végrehajtása adatbázison
            var list = await query.ToListAsync();

            // A lista átadása a nézetnek megjelenítésre
            return View(list);
        }

        // -------------------------------------------------------
        // RÉSZLETEK
        // -------------------------------------------------------

        // GET: /Property/Details/5
        // Egy konkrét ingatlan részletes adatait jeleníti meg
        public async Task<IActionResult> Details(int id)
        {
            // Lekérdezzük az ingatlant a megadott azonosító alapján,
            // betöltve a tulajdonos felhasználót és a képeket is
            var property = await _context.Properties
                .Include(p => p.TulajdonosUser)
                .Include(p => p.Kepek)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                // Ha nincs ilyen Id-jű ingatlan, 404-et (NotFound) adunk vissza
                return NotFound();
            }

            return View(property);
        }

        // -------------------------------------------------------
        // ÚJ HIRDETÉS LÉTREHOZÁSA
        // -------------------------------------------------------

        // GET: /Property/Create
        // Az űrlap megjelenítése új hirdetés létrehozásához
        [Authorize] // Csak bejelentkezett felhasználó adhat fel hirdetést
        public IActionResult Create()
        {
            // Egyszerűen csak visszaadjuk az üres űrlapot
            return View();
        }

        // POST: /Property/Create
        // Az űrlap elküldése után ez az akció hozza létre az új hirdetést
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF támadások elleni védelem
        [Authorize]
        public async Task<IActionResult> Create(Property model, List<IFormFile> kepek)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // Új Property objektum létrehozása a model alapján
            var property = new Property
            {
                Category = model.Category,
                Cim = model.Cim,
                Alapterulet = model.Alapterulet,
                Szobaszam = model.Szobaszam,
                Ar = model.Ar,
                ListingType = model.ListingType,
                Leiras = model.Leiras,
                Allapot = model.Allapot,
                Emelet = model.Emelet,
                EpitesEve = model.EpitesEve,
                Futestipus = model.Futestipus,
                Letrehozva = DateTime.UtcNow,
                TulajdonosUserId = user.Id
            };

            // Először magát a hirdetést mentjük, hogy legyen Id-je
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // ---------------------------
            // KÉPFELTÖLTÉS KEZELÉSE
            // ---------------------------

            // Ha a felhasználó küldött be fájlokat (nem kötelező)
            if (kepek != null && kepek.Count > 0)
            {
                // Megszámoljuk, hogy jelenleg hány kép tartozik ehhez az ingatlanhoz
                // (Create esetén ez 0 lesz, de Editnél számítana)
                int existingImageCount = await _context.PropertyImages
                    .CountAsync(pi => pi.PropertyId == property.Id);

                foreach (var file in kepek)
                {
                    // Ha a fájl null vagy üres, kihagyjuk
                    if (file == null || file.Length == 0)
                        continue;

                    // Ha elértük a maximális képszámot, kilépünk a ciklusból
                    if (existingImageCount >= MaxImagesPerProperty)
                    {
                        // Esetleg ModelState-hez is hozzáadhatnánk egy figyelmeztetést,
                        // de itt egyszerűen csak ignoráljuk a további képeket
                        break;
                    }

                    // Annak a mappának az elérési útja, ahova a képeket mentjük:
                    // wwwroot/images/properties/{PropertyId}/
                    var uploadRoot = Path.Combine(_env.WebRootPath, "images", "properties", property.Id.ToString());

                    // Ha a mappa még nem létezik, létrehozzuk
                    if (!Directory.Exists(uploadRoot))
                    {
                        Directory.CreateDirectory(uploadRoot);
                    }

                    // Eredeti fájlnévből csak a kiterjesztést használjuk fel (pl. .jpg, .png)
                    var extension = Path.GetExtension(file.FileName);

                    // Egyedi fájlnév generálása (GUID), hogy ne ütközzen másik fájllal
                    var fileName = Guid.NewGuid().ToString("N") + extension;

                    // A fájl teljes fizikai útvonala
                    var filePath = Path.Combine(uploadRoot, fileName);

                    // Fájl mentése a szerverre
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Relatív elérési útvonal az ImagePath mezőhöz
                    // Ezt fogjuk a <img src="...">-hez használni a nézetekben
                    var relativePath = $"/images/properties/{property.Id}/{fileName}";

                    // Új PropertyImage sor hozzáadása az adatbázishoz
                    var imageEntity = new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImagePath = relativePath
                    };

                    _context.PropertyImages.Add(imageEntity);

                    existingImageCount++;
                }

                // A képek mentésének végén elmentjük az adatbázis módosításokat
                await _context.SaveChangesAsync();
            }

            // Sikeres mentés után visszairányítjuk a felhasználót a listára
            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------
        // KÉP TÖRLÉSE EGY HIRDETÉSRŐL
        // -------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            // Lekérjük a képet az adatbázisból, betöltve a hozzá tartozó ingatlant is
            var image = await _context.PropertyImages
                .Include(pi => pi.Property)
                .FirstOrDefaultAsync(pi => pi.Id == imageId);

            if (image == null)
            {
                // Ha nincs ilyen kép, 404
                return NotFound();
            }

            // Az ingatlan, amihez a kép tartozik
            var property = image.Property;

            if (property == null)
            {
                return NotFound();
            }

            // Bejelentkezett user lekérése
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Ha valamiért nincs user, akkor loginra küldjük
                return Challenge();
            }

            // Jogosultság ellenőrzése:
            // - admin bármit törölhet
            // - nem admin csak a saját hirdetéséhez tartozó képet törölhet
            if (!User.IsInRole("Admin") && property.TulajdonosUserId != user.Id)
            {
                return Forbid();
            }

            // ---------------------------------
            // Fájl törlése a fájlrendszerből
            // ---------------------------------

            // Az ImagePath relatív út (pl. /images/properties/5/valami.jpg)
            var relativePath = image.ImagePath;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                // Leszedjük az elejéről az esetleges előre perjelet (/)
                var trimmedPath = relativePath.TrimStart('/');

                // Fizikai elérési út: wwwroot + relatív út
                var fullPath = Path.Combine(_env.WebRootPath, trimmedPath.Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            // ---------------------------------
            // Kép törlése az adatbázisból
            // ---------------------------------

            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync();

            // Visszairányítás a hirdetés Részletek oldalára
            return RedirectToAction("Details", new { id = property.Id });
        }

        // -------------------------------------------------------
        // HIRDETÉS SZERKESZTÉSE
        // -------------------------------------------------------

        // GET: /Property/Edit/5
        // Az űrlap megjelenítése egy meglévő hirdetés szerkesztéséhez
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            // (opcionális üzleti logika) csak a saját hirdetését módosíthatja a user,
            // admin bármelyiket – ezt itt később finomíthatjuk
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Ha nem admin és nem ő a tulaj, akkor tiltjuk
            if (!User.IsInRole("Admin") && property.TulajdonosUserId != user.Id)
            {
                return Forbid();
            }

            return View(property);
        }

        // POST: /Property/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
   
        public async Task<IActionResult> Edit(int id, Property model, List<IFormFile> kepek)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!User.IsInRole("Admin") && property.TulajdonosUserId != user.Id)
            {
                return Forbid();
            }

            // Mezők frissítése
            property.Category = model.Category;
            property.Cim = model.Cim;
            property.Alapterulet = model.Alapterulet;
            property.Szobaszam = model.Szobaszam;
            property.Ar = model.Ar;
            property.ListingType = model.ListingType;
            property.Leiras = model.Leiras;
            property.Allapot = model.Allapot;
            property.Emelet = model.Emelet;
            property.EpitesEve = model.EpitesEve;
            property.Futestipus = model.Futestipus;

            // EF Core-nak jelezzük, hogy módosult entitásról van szó
            _context.Properties.Update(property);

            // Módosítások mentése
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

            // ---------------------------
            // ÚJ KÉPEK HOZZÁADÁSA EDITNÉL
            // ---------------------------

            if (kepek != null && kepek.Count > 0)
            {
                int existingImageCount = await _context.PropertyImages
                    .CountAsync(pi => pi.PropertyId == property.Id);

                foreach (var file in kepek)
                {
                    if (file == null || file.Length == 0)
                        continue;

                    if (existingImageCount >= MaxImagesPerProperty)
                    {
                        break;
                    }

                    var uploadRoot = Path.Combine(_env.WebRootPath, "images", "properties", property.Id.ToString());

                    if (!Directory.Exists(uploadRoot))
                    {
                        Directory.CreateDirectory(uploadRoot);
                    }

                    var extension = Path.GetExtension(file.FileName);
                    var fileName = Guid.NewGuid().ToString("N") + extension;
                    var filePath = Path.Combine(uploadRoot, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var relativePath = $"/images/properties/{property.Id}/{fileName}";

                    var imageEntity = new PropertyImage
                    {
                        PropertyId = property.Id,
                        ImagePath = relativePath
                    };

                    _context.PropertyImages.Add(imageEntity);
                    existingImageCount++;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // -------------------------------------------------------
        // HIRDETÉS TÖRLÉSE
        // -------------------------------------------------------

        // GET: /Property/Delete/5
        // Törlés megerősítése
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties
                .Include(p => p.TulajdonosUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!User.IsInRole("Admin") && property.TulajdonosUserId != user.Id)
            {
                return Forbid();
            }

            return View(property);
        }

        // POST: /Property/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!User.IsInRole("Admin") && property.TulajdonosUserId != user.Id)
            {
                return Forbid();
            }

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
