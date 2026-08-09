using System.Collections.Generic;
using System.Linq;

namespace customerseller.Helpers
{
    // Yeh class AddProduct.cshtml ke JS "subCategoryMap" ka exact C# mirror hai.
    // Bulk upload ke waqt Excel me di gayi SubCategory value ko validate karne
    // aur uski Main Category pata karne ke liye use hoti hai.
    public static class CategoryMap
    {
        public static readonly Dictionary<string, List<string>> MainToSub = new()
        {
            ["FabricTextile"] = new() { "CushionCovers", "PrintedKurtas", "TieDyeDupattas", "FabricToteBags", "FabricTextile" },
            ["WoolYarn"] = new() { "Sweaters", "CrochetBags", "StorageBaskets", "Beanies", "WoolYarn" },
            ["Leather"] = new() { "LeatherHandbags", "Wallets", "Belts", "LeatherKeychains", "Journals", "Leather" },
            ["Wood"] = new() { "PhotoFrames", "WoodNecklaces", "WoodToys", "ServingTrays", "WoodShowpieces", "WoodKeychains", "Wood" },
            ["ClayPottery"] = new() { "FlowerPots", "CoffeeMugs", "ClayVases", "ClayEarrings", "ClayFigurines", "ClayPottery" },
            ["Jewels"] = new() { "Necklaces", "Bracelets", "Rings", "Bangles", "JewelEarrings", "Jewels" },
            ["PaperCards"] = new() { "GreetingCards", "GiftBoxes", "PaperFlowers", "PaperShowpieces", "Scrapbooks", "PaperCards" },
            ["JuteNatural"] = new() { "JuteBags", "Baskets", "Trays", "DriedBouquets", "Paintings", "JuteNatural" },
            ["ResinEpoxy"] = new() { "Coasters", "ResinEarrings", "ResinKeychains", "WallArt", "ResinServingTrays", "ResinEpoxy" },
            ["MetalWire"] = new() { "MetalEarrings", "MetalVases", "MetalFigurines", "MetalWallArt", "CandleHolders", "MetalWire" },
        };

        // SubCategory code -> Main Category code (reverse lookup, banaya ek dafa static constructor me)
        public static readonly Dictionary<string, string> SubToMain =
            MainToSub.SelectMany(kv => kv.Value.Select(sub => new { Main = kv.Key, Sub = sub }))
                     .ToDictionary(x => x.Sub, x => x.Main);

        public static bool IsValidSubCategory(string subCategory) =>
            !string.IsNullOrWhiteSpace(subCategory) && SubToMain.ContainsKey(subCategory);

        public static string GetMainCategory(string subCategory) =>
            SubToMain.TryGetValue(subCategory, out var main) ? main : null;
    }
}
