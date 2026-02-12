using househub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace househub.Controllers
{
    // Ez a controller a bejelentkezett felhasználó saját fiókbeállításait kezeli.
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // DI-ből megkapjuk az Identity szolgáltatásokat.
        public SettingsController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // GET: /Settings
        // A beállítások oldal megjelenítése az aktuális felhasználó adataival.
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Ha valamiért nincs user objektum, loginra terelünk.
                return Challenge();
            }

            var model = new AccountSettingsViewModel
            {
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                UserType = user.UserType
            };

            return View(model);
        }

        // POST: /Settings
        // Profil adatok + opcionális jelszócsere mentése.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AccountSettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Csak Tulajdonos vagy Ingatlanos típus választható ezen az oldalon.
            if (model.UserType is not UserType.Tulajdonos and not UserType.Ingatlanos)
            {
                ModelState.AddModelError(nameof(model.UserType), "Itt csak Tulajdonos vagy Ingatlanos típus választható.");
            }

            // Jelszócsere csak akkor induljon, ha bármelyik jelszó mező ki van töltve.
            var wantsPasswordChange =
                !string.IsNullOrWhiteSpace(model.CurrentPassword) ||
                !string.IsNullOrWhiteSpace(model.NewPassword) ||
                !string.IsNullOrWhiteSpace(model.ConfirmNewPassword);

            // Ha jelszót akar cserélni, minden szükséges mezőt kérjünk be.
            if (wantsPasswordChange)
            {
                if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                {
                    ModelState.AddModelError(nameof(model.CurrentPassword), "A jelenlegi jelszó megadása kötelező jelszócseréhez.");
                }

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    ModelState.AddModelError(nameof(model.NewPassword), "Az új jelszó megadása kötelező.");
                }

                if (string.IsNullOrWhiteSpace(model.ConfirmNewPassword))
                {
                    ModelState.AddModelError(nameof(model.ConfirmNewPassword), "Az új jelszó megerősítése kötelező.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // -----------------------------
            // Alap profil adatok frissítése
            // -----------------------------

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }

                // A usernév nálunk legyen szinkronban az emaillel.
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.Email);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            if (!string.Equals(user.PhoneNumber, model.PhoneNumber, StringComparison.Ordinal))
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    foreach (var error in setPhoneResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            // Saját UserType mező frissítése (nem azonos az Identity role-lal).
            if (user.UserType != model.UserType)
            {
                user.UserType = model.UserType;
                var updateUserResult = await _userManager.UpdateAsync(user);

                if (!updateUserResult.Succeeded)
                {
                    foreach (var error in updateUserResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            // ---------------------------------------------
            // Identity szerepkörök igazítása a UserType-hoz
            // ---------------------------------------------

            var rolesToRemove = new[] { "Tulajdonos", "Ingatlanos" };
            foreach (var role in rolesToRemove)
            {
                if (await _userManager.IsInRoleAsync(user, role))
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
            }

            var roleToAdd = model.UserType == UserType.Ingatlanos ? "Ingatlanos" : "Tulajdonos";

            if (!await _roleManager.RoleExistsAsync(roleToAdd))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleToAdd));
            }

            await _userManager.AddToRoleAsync(user, roleToAdd);

            // -----------------------------
            // Opcionális jelszócsere kezelése
            // -----------------------------

            if (wantsPasswordChange)
            {
                var changePasswordResult = await _userManager.ChangePasswordAsync(
                    user,
                    model.CurrentPassword!,
                    model.NewPassword!);

                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return View(model);
                }
            }

            // Fontos: adatmódosítás után frissítsük a bejelentkezési sütit.
            await _signInManager.RefreshSignInAsync(user);

            TempData["SettingsMessage"] = "A beállítások mentése sikeres volt.";
            return RedirectToAction(nameof(Index));
        }
    }
}
