using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homecat.Models;                          // ApplicationUser, Property, UserType
using Homecat.Data;                            // ApplicationDbContext
using Microsoft.AspNetCore.Authorization;      // [Authorize]
using Microsoft.AspNetCore.Identity;           // UserManager, IdentityRole
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;           // Include, ToListAsync

namespace Homecat_P.Controllers                  // FIGYELEM: ha a többi controller namespace-e Homecat.Controllers, akkor ezt írd!
{
    // Ez a controller az admin felületet valósítja meg.
    // Csak az "Admin" szerepkörrel rendelkező felhasználók érhetik el.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Konstruktor – DI adja be az adatbázis kontextust és a UserManager-t
        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // -------------------------------------------------------
        // FELHASZNÁLÓK LISTÁJA
        // -------------------------------------------------------

        // GET: /Admin/Users
        // Minden felhasználó listázása a rendszerben,
        // hozzárendelt felhasználó típussal (UserType) és szerepkörökkel (Roles).
        public async Task<IActionResult> Users()
        {
            // Lekérdezzük az összes felhasználót az AspNetUsers táblából
            var users = await _userManager.Users.ToListAsync();

            // Létrehozunk egy egyszerű ViewModel listát,
            // amiben benne lesz a user + a szerepkörei.
            var model = new List<AdminUserListItemViewModel>();

            foreach (var user in users)
            {
                // Minden userhez lekérdezzük az Identity szerepköreit
                var roles = await _userManager.GetRolesAsync(user);

                model.Add(new AdminUserListItemViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    UserType = user.UserType,
                    Roles = roles.ToList()
                });
            }

            return View(model);
        }

        // -------------------------------------------------------
        // HIRDETÉSEK LISTÁJA (ADMIN)
        // -------------------------------------------------------

        // GET: /Admin/Properties
        // Minden ingatlan hirdetés listázása, tulajdonos adataival.
        public async Task<IActionResult> Properties()
        {
            // Lekérdezzük az összes ingatlant, betöltve a tulajdonos felhasználót is
            var properties = await _context.Properties
                .Include(p => p.TulajdonosUser)
                .OrderByDescending(p => p.Letrehozva)
                .ToListAsync();

            return View(properties);
        }

        // -------------------------------------------------------
        // (OPCIONÁLIS) HIRDETÉS TÖRLÉSE ADMIN OLDALRÓL
        // -------------------------------------------------------

        // GET: /Admin/DeleteProperty/5
        public async Task<IActionResult> DeleteProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.TulajdonosUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // POST: /Admin/DeleteProperty/5
        [HttpPost, ActionName("DeleteProperty")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePropertyConfirmed(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            // Egyszerű verzió: csak az ingatlan törlése az adatbázisból.
            // (Képeket is le lehetne szedni, ahogy a DeleteImage-nél csináltuk.)
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Properties));
        }
    }

    // -------------------------------------------------------
    // EGYSZERŰ VIEWMODEL A FELHASZNÁLÓ LISTÁHOZ
    // -------------------------------------------------------
    public class AdminUserListItemViewModel
    {
        // Felhasználó azonosító (AspNetUsers.Id)
        public string UserId { get; set; } = string.Empty;

        // Email cím
        public string Email { get; set; } = string.Empty;

        // Felhasználónév
        public string UserName { get; set; } = string.Empty;

        // Saját UserType mező (Tulajdonos / Ingatlanos / Admin)
        public UserType UserType { get; set; }

        // Identity szerepkörök listája (pl. Admin, Tulajdonos, Ingatlanos)
        public List<string> Roles { get; set; } = new List<string>();
    }
}
        