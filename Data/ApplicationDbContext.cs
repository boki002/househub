using househub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace househub.Data
{
    // Az alkalmazás EF Core kontextusa
    // IdentityDbContext<ApplicationUser> = tartalmazza az Identity-táblákat is
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Konstruktor, amely átadja az opciókat az ősnek
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Az ingatlan hirdetéseket reprezentáló tábla
        public DbSet<Property> Properties { get; set; }

        // Az ingatlan képeket reprezentáló tábla
        public DbSet<PropertyImage> PropertyImages { get; set; }

        // Itt tudjuk finomhangolni a modellezést (tábla nevek, kapcsolatok, stb.)
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Először meghívjuk az Identity alap beállításait
            base.OnModelCreating(builder);

            // Property - PropertyImage kapcsolat: 1 ingatlanhoz több kép tartozhat
            builder.Entity<Property>()
                .HasMany(p => p.Kepek)
                .WithOne(i => i.Property!)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade); // Ha törlünk egy hirdetést, a képei is törlődnek

            // TulajdonosUserId => ApplicationUser kapcsolat (1 felhasználónak több hirdetése lehet)
            builder.Entity<Property>()
                .HasOne(p => p.TulajdonosUser)
                .WithMany() // egyszerűsítés: most nem definiálunk visszairányú gyűjteményt
                .HasForeignKey(p => p.TulajdonosUserId)
                .OnDelete(DeleteBehavior.Restrict); // Ne töröljük automatikusan a felhasználót, ha hirdetés van hozzá
        }
    }
}
