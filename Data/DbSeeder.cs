using System;
using System.Threading.Tasks;
using Homecat.Models;                       // ApplicationUser eléréséhez
using Microsoft.AspNetCore.Identity;        // RoleManager, UserManager

namespace Homecat.Data                    
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            // 1. Szerepkörök létrehozása

            string[] roles = { "Admin", "Ingatlanos", "Tulajdonos" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Admin felhasználó létrehozása

            string adminEmail = "admin@homecat.local";
            string adminPassword = "Admin123";

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    EmailConfirmed = true,
                    UserType = UserType.Admin   // saját enum mezőnk
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}
