using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Homecat.Models
{
    // Ingatlan kategória (lakás, ház, telek, iroda)
    public enum PropertyCategory
    {
        Lakás = 0,
        Ház = 1,
        Telek = 2,
        Iroda = 3
    }

    // Hirdetés típusa: eladó vagy kiadó (bérlés)
    public enum ListingType
    {
        Elado = 0,
        Berles = 1
    }

    // Ingatlan állapota (pl. új építésű, felújított, stb.)
    public enum PropertyCondition
    {
        Ismeretlen = 0,
        UjEpitesu = 1,
        Felujitott = 2,
        JoAllapotu = 3,
        KozepesAllapotu = 4,
        Felujitando = 5
    }

    // Fűtés típusa
    public enum HeatingType
    {
        Ismeretlen = 0,
        GázCirkó = 1,
        Távfűtés = 2,
        Villany = 3,
        Vegyestüzelésű = 4,
        Padlófűtés = 5
    }

    // Ez az entitás egy ingatlan hirdetést reprezentál az adatbázisban
    public class Property
    {
        // Elsődleges kulcs (egyedi azonosító)
        public int Id { get; set; }

        // Ingatlan kategória (lakás / ház / telek / iroda)
        [Required]
        public PropertyCategory Category { get; set; }

        // Cím – egyszerűsítve egyetlen stringben tároljuk
        [Required]
        [MaxLength(200)]
        public string Cim { get; set; } = string.Empty;

        // Alapterület négyzetméterben
        [Required]
        [Range(1, 100000)]
        public int Alapterulet { get; set; }

        // Szobák száma (pl. 1, 2, 3, stb.)
        [Required]
        [Range(0, 50)]
        public int Szobaszam { get; set; }

        // Ár forintban (egyszerűen int-ként, de lehetne decimal is)
        [Required]
        [Range(0, int.MaxValue)]
        public int Ar { get; set; }

        // Hirdetés típusa: eladó vagy bérlés
        [Required]
        public ListingType ListingType { get; set; }

        // Szabad szöveges leírás az ingatlanról
        [MaxLength(2000)]
        public string? Leiras { get; set; }

        // Ingatlan állapota (új építésű, felújított, stb.)
        public PropertyCondition Allapot { get; set; } = PropertyCondition.Ismeretlen;

        // Emelet (pl. 0 = földszint, 1, 2, stb.)
        // Telek vagy ház esetén akár üresen is maradhat
        public int? Emelet { get; set; }

        // Építés éve (pl. 1990, 2005, stb.)
        public int? EpitesEve { get; set; }

        // Fűtés típusa (gázcirkó, távfűtés, stb.)
        public HeatingType Futestipus { get; set; } = HeatingType.Ismeretlen;

        // A hirdetés létrehozásának dátuma (lista rendezéséhez jól jön)
        public DateTime Letrehozva { get; set; } = DateTime.UtcNow;

        // Annak a felhasználónak az azonosítója, aki a hirdetést létrehozta
        // ASP.NET Identity User Id (string)
        public string TulajdonosUserId { get; set; } = string.Empty;

        // Navigációs tulajdonság – felhasználó, akié a hirdetés
        public ApplicationUser? TulajdonosUser { get; set; }

        // Navigációs tulajdonság – az ingatlanhoz tartozó képek listája
        public List<PropertyImage> Kepek { get; set; } = new List<PropertyImage>();
    }

    // Ez az entitás az adott ingatlanhoz tartozó egy képet reprezentál
    public class PropertyImage
    {
        // Elsődleges kulcs
        public int Id { get; set; }

        // Az adott kép fájlneve vagy relatív elérési útja
        // (pl. /images/properties/1234_1.jpg)
        [Required]
        [MaxLength(255)]
        public string ImagePath { get; set; } = string.Empty;

        // Külső kulcs az ingatlanra (Property.Id)
        public int PropertyId { get; set; }

        // Navigációs tulajdonság – az a hirdetés, amihez a kép tartozik
        public Property? Property { get; set; }
    }
}
