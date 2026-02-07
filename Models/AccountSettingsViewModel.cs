using System.ComponentModel.DataAnnotations;

namespace househub.Models
{
    // Ez a ViewModel a "Beállítások" oldal űrlapjához kell.
    // Közvetlenül nem adatbázis entitás, csak a nézet és a controller közti adatcsere.
    public class AccountSettingsViewModel
    {
        // A felhasználó email címe (bejelentkezési azonosítóként is használjuk).
        [Required(ErrorMessage = "Az email cím megadása kötelező.")]
        [EmailAddress(ErrorMessage = "Adj meg egy érvényes email címet.")]
        [Display(Name = "Email cím")]
        public string Email { get; set; } = string.Empty;

        // Opcionális telefonszám.
        [Phone(ErrorMessage = "Adj meg érvényes telefonszámot.")]
        [Display(Name = "Telefonszám")]
        public string? PhoneNumber { get; set; }

        // A felhasználó által választott típus (csak Tulajdonos vagy Ingatlanos lehet itt).
        [Display(Name = "Felhasználó típus")]
        public UserType UserType { get; set; }

        // A jelenlegi jelszó akkor kell, ha jelszót akar cserélni a felhasználó.
        [DataType(DataType.Password)]
        [Display(Name = "Jelenlegi jelszó")]
        public string? CurrentPassword { get; set; }

        // Új jelszó (opcionális, csak jelszócsere esetén töltjük).
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Az új jelszó legalább 6 karakter legyen.")]
        [Display(Name = "Új jelszó")]
        public string? NewPassword { get; set; }

        // Új jelszó megerősítése.
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "A két új jelszó nem egyezik.")]
        [Display(Name = "Új jelszó újra")]
        public string? ConfirmNewPassword { get; set; }
    }
}
