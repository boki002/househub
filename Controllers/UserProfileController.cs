using System.Threading.Tasks;
using Homecat.Models;                      // ApplicationUser, UserType
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Homecat_P.Controllers            // ha a többi controller namespace-e Homecat, akkor azt használd
{
    // Ez a controller kezeli a bejelentkezett felhasználó profilját,
    // konkrétan a felhasználó típusának (Tulajdonos / Ingatlanos) beállítását.
    [Authorize] // Csak bejelentkezett felhasználók érhetik el
    public class UserProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // A UserManager és RoleManager szolgáltatásokat DI adja be
        public UserProfileController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // -----------------------------------------------------
        // GET: /UserProfile/EditRole
        // Az űrlap megjelenítése, ahol a user kiválaszthatja
        // hogy Tulajdonos vagy Ingatlanos legyen.
        // -----------------------------------------------------
        public async Task<IActionResult> EditRole()
        {
            // Bejelentkezett felhasználó lekérése
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                // Ha valami gond van, visszaküldjük loginra
                return Challenge();
            }

            // A mostani UserType értéket átadjuk a ViewData-n keresztül,
            // hogy a nézetben be tudjuk jelölni az aktuális állapotot.
            ViewData["CurrentUserType"] = user.UserType;

            return View();
        }

        // -----------------------------------------------------
        // POST: /UserProfile/EditRole
        // A kiválasztott felhasználó típus mentése:
        // - UserType mező frissítése ApplicationUser-ben
        // - Identity szerepkörök (Tulajdonos / Ingatlanos) aktualizálása
        // -----------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(UserType userType)
        {
            // Bejelentkezett felhasználó lekérése
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // Elmentjük az új típust az adatbázisban
            user.UserType = userType;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                // Ha valami hiba történik a mentéskor, hozzáadhatnánk ModelState hibát,
                // de egyszerűség kedvéért csak visszadobjuk a nézetre.
                ViewData["CurrentUserType"] = user.UserType;
                return View();
            }

            // ------------------------------------------
            // Identity szerepkörök frissítése
            // ------------------------------------------

            // Először levesszük a felhasználót a "Tulajdonos" és "Ingatlanos" szerepkörökből,
            // hogy ne lehessen egyszerre mindkettő.
            var rolesToRemove = new[] { "Tulajdonos", "Ingatlanos" };

            foreach (var role in rolesToRemove)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            // Ezután hozzárendeljük a kiválasztott típus szerinti szerepkörhöz
            string roleToAdd = userType == UserType.Ingatlanos ? "Ingatlanos" : "Tulajdonos";

            // Biztonság kedvéért megnézzük, létezik-e a szerepkör (DbSeeder-nek már létre kellett hoznia)
            if (!await _roleManager.RoleExistsAsync(roleToAdd))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleToAdd));
            }

            await _userManager.AddToRoleAsync(user, roleToAdd);

            // Visszairányítjuk mondjuk az ingatlan listára, vagy a főoldalra
            return RedirectToAction("Index", "Property");
        }
    }
}
