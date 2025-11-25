1. Alap projekt és adatbázis

ASP.NET Core MVC + Entity Framework Core + MariaDB integráció létrehozva

ApplicationDbContext működik, MariaDB-hez csatlakozik

Properties, PropertyImages, AspNetUsers, AspNetRoles táblák létrejöttek

wwwroot/images/properties/{id}/ fájlstruktúra bevezetve a képekhez

2. Identity rendszer kiterjesztése

ApplicationUser modell bővítve:

UserType enum (Tulajdonos, Ingatlanos, Admin)

DbSeeder létrehozza az alap Identity szerepköröket:

Admin, Tulajdonos, Ingatlanos

A regisztrációs oldal helyett saját szerepkör-váltó felület készült:

UserProfileController + EditRole nézet

bejelentkezett felhasználó módosíthatja saját típusát

automatikus Identity szerepkör-kezelés

3. Ingatlan funkciók (CRUD + képek)

Property modell minden szükséges adattal (ár, cím, kategória, leírás, stb.)

PropertyImage modell több kép támogatásához

PropertyController:

hirdetés létrehozása (Create)

max 10 kép feltöltése

képek mentése fizikailag a wwwroot-ba

rekordok mentése a DB-be

hirdetés szerkesztése (Edit)

új képek hozzáadása

hirdetés részletei (Details)

képek megjelenítése galéria formában

kép törlése (DeleteImage)

csak tulajdonos vagy admin törölhet

törli a DB-ből és a fájlrendszerből is

4. Főoldal és UI

Főoldal (Home/Index) átalakítva:

legutóbbi 6 ingatlan listázása Bootstrap kártyákkal

első kép thumbnailként

_Layout.cshtml menü bővítve:

„Ingatlanok” menüpont

„Szerepkör / felhasználó típus” menüpont bejelentkezett usernek

Hibák javítva (ErrorViewModel, namespace-ek, UserManager-IdentityUser típusok átírása)

5. Jogosultságok

Csak bejelentkezett felhasználó adhat fel hirdetést

Kép törlése: csak tulajdonos vagy admin végezheti

UserType + Identity Role mindig szinkronban van

Admin
