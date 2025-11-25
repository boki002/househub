using Microsoft.AspNetCore.Identity;

namespace Homecat.Models
{
    // A felhasználó típusát jelző enum
    public enum UserType
    {
        Tulajdonos = 0,
        Ingatlanos = 1,
        Admin = 2
    }

    // Saját Identity felhasználó osztály, amely kiegészíti az alap IdentityUser-t
    public class ApplicationUser : IdentityUser
    {
        // A felhasználó szerepe (tulajdonos / ingatlanos / admin)
        // Fontos: ez nem azonos az Identity Role rendszerével,
        // csak egy plusz mező, ami egyszerű logikához elég lehet.
        public UserType UserType { get; set; } = UserType.Tulajdonos;
    }
}
