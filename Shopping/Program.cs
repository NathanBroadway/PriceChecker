using Humanizer;
using Humanizer.Inflections;
using Newtonsoft.Json;
using Shopping.Extensions;
using Shopping.Models;
using System.Diagnostics;

// Currently, 4 dinners, 3 lunches, 2 breakfasts
internal class Program
{
    private const string _staples = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\Staples.txt";
    private const string _shoppingList = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\ShoppingList.txt";
    private static string _jsonStorePlan = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\StorePlan_" + GetShoppingDay() + ".json";
    private static string _historicalGroceries = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\HistoricalGroceries" + ".json";
    private static string _textStorePlan = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\StorePlan_" + GetShoppingDay() + ".txt";
    private static string _subsitutes = @"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\Subsitutes.json";

    private static string GetShoppingDay()
    {
        for (int i = 0; i < 7; i++)
        {
            var shoppingDay = DateTime.Now.AddDays(i);
            if (shoppingDay.DayOfWeek == DayOfWeek.Tuesday)
            {
                return shoppingDay.Year + "_" + shoppingDay.Month + "_" + shoppingDay.Day;
            }
        }
        return DateTime.Now.ToString();
    }
    private static void Main(string[] args)
    {
        SetSingularExceptions();
        var groceries = GetGroceryList();
        var historicalGroceries = GetHistoricalGroceryList();
        groceries.QuestionList();
        groceries = ReplaceToDomainLanguage(groceries);
        CheckForSubstitutes(groceries);
        var stores = GetExistingStorePlan();
        var groceryCopy = groceries.Select(x => x).ToList();
        groceries = RemovePlannnedGrocery(groceries.ToList(), stores);
        GroceryFinder(groceries, historicalGroceries, stores);
        WriteCurrentList(stores);
        WriteHistoricalGroceriesList(stores, historicalGroceries);

        BuildCart(groceryCopy, historicalGroceries, stores);
    }

    private static void CheckForSubstitutes(GroceryItem[] groceries)
    {
        var jsonString = File.ReadAllText(_subsitutes);
        var substitutes = JsonConvert.DeserializeObject<List<Substitutes>>(jsonString);
        foreach (var substitute in substitutes)
        {
            foreach (var grocery in groceries)
            {
                if (grocery.Name.ToLower() == substitute.Name.ToLower() || grocery.Name.ToLower().Contains(substitute.Name.ToLower()) || substitute.Name.ToLower().Contains(grocery.Name.ToLower()))
                {
                    Console.WriteLine($"Check on this subtitute {substitute.Name} to replace {grocery.Name}\n\t{substitute.Amount}\n\t{substitute.Substitute}\n\n");
                }
            }
        }

        Console.ReadKey();
        Console.Clear();
    }

    private static void BuildCart(List<GroceryItem> groceries, List<HistoricalGroceryItem> historicalGroceries, List<Store> stores)
    {
        foreach (var grocery in groceries)
        {
            Console.WriteLine($"Please add " + grocery.Amount + " of " + grocery.Name);
            var groceryItem = new GroceryItem { Amount = grocery.Amount, Name = grocery.Name };
            var historicalGroceryItem = historicalGroceries.FirstOrDefault(historicalGrocery => historicalGrocery.Name == grocery.Name);
            DeterminePreferredStoreFromHistoricals(stores, groceryItem, historicalGroceryItem);
            Console.Clear();
        }
    }

    private static void SetSingularExceptions()
    {
        Vocabularies.Default.AddUncountable("hummus");
        Vocabularies.Default.AddUncountable("soda");
        Vocabularies.Default.AddUncountable("fries");
        Vocabularies.Default.AddUncountable("pasta");
        Vocabularies.Default.AddUncountable("whole-grain pasta");
        Vocabularies.Default.AddUncountable("orzo pasta");
        Vocabularies.Default.AddUncountable("kalamata olives");
        Vocabularies.Default.AddUncountable("dairy-free");
        Vocabularies.Default.AddUncountable("alphabet pasta");
        Vocabularies.Default.AddUncountable("apples");
        Vocabularies.Default.AddUncountable("precooked polenta");
        Vocabularies.Default.AddUncountable("asparagus");
        Vocabularies.Default.AddUncountable("baking soda");
        Vocabularies.Default.AddUncountable("couscous");
        Vocabularies.Default.AddUncountable("penne pasta");
        Vocabularies.Default.AddUncountable("whole-grain penne pasta");
    }
    private static void GroceryFinder(GroceryItem[] groceries, List<HistoricalGroceryItem> historicalGroceries, List<Store> stores)
    {

        foreach (var grocery in groceries)
        {
            var groceryItem = new GroceryItem { Amount = grocery.Amount, Name = grocery.Name };
            var historicalGroceryItem = historicalGroceries.FirstOrDefault(historicalGrocery => historicalGrocery.Name == grocery.Name);
            
            Console.WriteLine("Price " + groceryItem.Amount + " of " + groceryItem.Name);
            groceryItem = DeterminePreferredStoreFromHistoricals(stores, groceryItem, historicalGroceryItem);

            HandleUnpricedStores(stores, grocery, groceryItem);

            groceryItem.SetAmount();
            groceryItem.SetNotes();

            var preferredStore = groceryItem.GetLowestPricedStore();
            if (preferredStore == null)
            {
                Console.Clear();
                continue;
            }
            //FindCoupons(groceryItem);

            stores.AddGroceryItemToSelectedStore(preferredStore.StoreName, groceryItem);
            WriteCurrentList(stores);
            WriteHistoricalGroceriesList(stores, historicalGroceries);

            Console.Clear();
        }
    }
    private static void HandleUnpricedStores(List<Store> stores, GroceryItem grocery, GroceryItem groceryItem)
    {
        var storesFiltered = stores.Where(store => !groceryItem.Prices.Select(grocery => grocery.StoreName).ToList().Contains(store.Name));
        var runningCheapestPrice = grocery.GetLowestPricedStore()?.Price ?? 999m;
        foreach (var store in storesFiltered)
        {
            store.OpenStore(grocery.Name, runningCheapestPrice);
            Console.Write($"Price at {store.Name}: ");
            var newPrice = groceryItem.SetPrice(store.Name);
            if (newPrice != null && runningCheapestPrice > newPrice)
            {
                runningCheapestPrice = newPrice.Value;
            }
        }
    }
    private static GroceryItem DeterminePreferredStoreFromHistoricals(List<Store> stores, GroceryItem groceryItem, HistoricalGroceryItem? historicalGroceryItem)
    {
        if (historicalGroceryItem == null || historicalGroceryItem.Prices.Count == 0)
        {
            return groceryItem;
        }

        StorePrice previousPreferredStore;
        do
        {
            previousPreferredStore = historicalGroceryItem.GetLowestPricedStore()!;
            Console.WriteLine("Is " + previousPreferredStore.StoreName + " the cheapest store?");
            var prevPreferredStore = stores.Where(store => store.Name == previousPreferredStore.StoreName).First();
            prevPreferredStore.OpenStore(historicalGroceryItem.Name, previousPreferredStore.Price);
            Console.Write($"Price at {previousPreferredStore.StoreName}: ");
            historicalGroceryItem.SetPrice(prevPreferredStore.Name);
            historicalGroceryItem.DatePrice = DateTime.Now.Date;
        } while (previousPreferredStore.Price < historicalGroceryItem.GetLowestPricedStore()?.Price);

        groceryItem.Prices = historicalGroceryItem.Prices;
        groceryItem.Notes = historicalGroceryItem.Notes;
        return groceryItem;
    }
    private static void WriteHistoricalGroceriesList(List<Store> stores, List<HistoricalGroceryItem> historicalGroceries)
    {
        foreach (var store in stores)
        {
            foreach (var groceryItem in store.Items)
            {
                if (historicalGroceries.Any(historicalGrocery => historicalGrocery.Name == groceryItem.Name))
                {
                    var historicalGrocery = historicalGroceries.First(historicalGrocery => historicalGrocery.Name == groceryItem.Name);
                    historicalGrocery.DatePrice = DateTime.UtcNow;
                    historicalGrocery.Prices = groceryItem.Prices;
                }
                else
                {
                    historicalGroceries.Add(new HistoricalGroceryItem
                    {
                        Name = groceryItem.Name,
                        Amount = groceryItem.Amount,
                        Notes = groceryItem.Notes,
                        Prices = groceryItem.Prices,
                        StoreName = store.Name,
                        DatePrice = DateTime.UtcNow,
                    });
                }
            }
        }
        File.WriteAllText(_historicalGroceries, JsonConvert.SerializeObject(historicalGroceries));
    }
    private static List<HistoricalGroceryItem> GetHistoricalGroceryList()
    {
        if (File.Exists(_historicalGroceries))
        {
            var jsonString = File.ReadAllText(_historicalGroceries);
            var historicalGroceries = JsonConvert.DeserializeObject<List<HistoricalGroceryItem>>(jsonString);
            historicalGroceries.RemoveAll(historicalGrocery => historicalGrocery.DatePrice < DateTime.UtcNow.AddDays(-31));
            foreach (var historicalGrocery in historicalGroceries)
            {
                historicalGrocery.Prices.RemoveAll(grocery => grocery.DateRecord < DateTime.UtcNow.AddDays(-31));
            }
            return historicalGroceries;
        }

        return new List<HistoricalGroceryItem> { };
    }
    private static GroceryItem[] ReplaceToDomainLanguage(GroceryItem[] groceries)
    {
        GroceryItem? groceryItem = null;
        if (GetGroceryItem_ExactMatch(groceries, null, "unflavored plant milk") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "unflavored plant milk");
            groceryItem!.Name = "almond milk";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "nectarines") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "nectarines");
            groceryItem!.Name = "peach";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "rainbow chard") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "rainbow chard");
            groceryItem!.Name = "chard";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "ready-to-eat shelled edamame") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "ready-to-eat shelled edamame");
            groceryItem!.Name = "edamame";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "white beans") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "white beans");
            groceryItem!.Name = "Cannellini Beans";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "fennel bulb") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "fennel bulb");
            groceryItem!.Name = "fennel";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "new red potato") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "new red potato");
            groceryItem!.Name = "red potato";
        }
        else if (GetGroceryItem_ExactMatch(groceries, null, "fresh or frozen mango") != null)
        {
            groceryItem = GetGroceryItem_ExactMatch(groceries, groceryItem, "fresh or frozen mango");
            groceryItem!.Name = "mango";
        }

        foreach (var grocery in groceries)
        {
            if (grocery.Name.StartsWith("fresh or frozen"))
            {
                grocery.Name = grocery.Name.Replace("fresh or frozen ", "frozen ");
            }
            if (grocery.Name.StartsWith("fresh"))
            {
                grocery.Name = grocery.Name.Replace("fresh ", "");
            }
            if (grocery.Name.Contains("whole-grain "))
            {
                grocery.Name = grocery.Name.Replace("whole-grain ", "");
            }
            if (grocery.Name.Contains("whole-wheat "))
            {
                grocery.Name = grocery.Name.Replace("whole-wheat ", "");
            }
            if (grocery.Name.StartsWith("low-sodium"))
            {
                grocery.Name = grocery.Name.Replace("low-sodium ", "");
            }
            grocery.Name = grocery.Name.Singularize(false);
        }
        return groceries;
    }
    private static GroceryItem? GetGroceryItem_ExactMatch(GroceryItem[] groceries, GroceryItem? groceryItem, string groceryName)
    {
        return groceryItem ?? groceries.FirstOrDefault(grocery => grocery.Name == groceryName);
    }
    private static GroceryItem[] RemovePlannnedGrocery(List<GroceryItem> groceries, List<Store> stores)
    {
        foreach (var store in stores)
        {
            foreach (var groceryItem in store.Items)
            {
                if (groceries.Any(grocery => grocery.Name == groceryItem.Name))
                {
                    groceries.Remove(groceries.Where(grocery => grocery.Name == groceryItem.Name).Single());
                }
            }
        }
        return groceries.ToArray();
    }
    private static List<Store> GetExistingStorePlan()
    {
        var storeList = new List<Store>()
        {
            new Store("Hy-Vee")
            {
                StoreUrl = @"https://www.hy-vee.com/aisles-online/search?search={{SEPERATECHAR}}&sortDirection=PRICE_ASCENDING",
                SeperateCharacter = "+"
            },
            new Store("Walmart")
            {
                StoreUrl= @"https://www.walmart.com/search?q=+{{SEPERATECHAR}}&sort=price_low&facet=fulfillment_method_in_store%3AIn-store&max_price={{CHEAPESTPRICE}}",
                SeperateCharacter = "+"
            },
            new Store("Target")
            {
                StoreUrl = @"https://www.target.com/s?searchTerm={{SEPERATECHAR}}&sortBy=PriceLow&moveTo=product-list-grid&facetedValue=5zl7w&ignoreBrandExactness=true",
                SeperateCharacter = "+"
            },
            new Store("Aldi")
            {
                StoreUrl = @"https://new.aldi.us/results?q={{SEPERATECHAR}}&sort=price_asc",
                SeperateCharacter = "%20"
            },
            new Store("Dillons")
            {
                StoreUrl= @"https://www.dillons.com/search?fulfillment=ais&price=0.1-{{CHEAPESTPRICE}}&query={{SEPERATECHAR}}&searchType=default_search",
                SeperateCharacter = "%20"
            },
            new Store("Checkers")
            {
                StoreUrl=@"https://checkersmarket.rosie.shop/checkers_lawrence/search/{{SEPERATECHAR}}",
                SeperateCharacter = "%20"
            },
        };

        if (File.Exists(_jsonStorePlan))
        {
            string jsonString = File.ReadAllText(_jsonStorePlan);
            storeList = JsonConvert.DeserializeObject<List<Store>>(jsonString);
        }

        if (storeList.Any(store => store.Name == "Grand"))
        {
            storeList.Remove(storeList.First(store => store.Name == "Grand"));
        }

        return storeList;
    }
    private static void WriteCurrentList(List<Store> storeList)
    {
        var grandTotal = 0m;
        var finalString = "";
        foreach (var store in storeList)
        {
            finalString += store.Name + ":";
            var storeTotal = 0m;
            foreach (var groceryItem in store.Items)
            {
                storeTotal += groceryItem.GetLowestPricedStore()!.Price.Value;
                finalString += "\n" + groceryItem.ToString();
            }
            grandTotal += storeTotal;
        }

        storeList.Add(new Store("Grand")
        {
            Total = grandTotal
        });

        File.WriteAllText(_textStorePlan, finalString);
        File.WriteAllText(_jsonStorePlan, JsonConvert.SerializeObject(storeList));
        storeList.Remove(storeList.First(store => store.Name == "Grand"));
    }
    private static GroceryItem[] GetGroceryList()
    {
        var groceryList = new List<string>();
        groceryList.AddRange(File.ReadAllLines(_staples));
        groceryList.AddRange(File.ReadAllLines(_shoppingList));
        var groceryItems = new List<GroceryItem>();
        foreach (var grocery in groceryList)
        {
            if (grocery.Contains(','))
            {
                var amount = grocery.Split(',')[0];
                var cloneGroceryPieces = grocery.Split(',').ToList();
                cloneGroceryPieces.Remove(cloneGroceryPieces.First());
                groceryItems.Add(new GroceryItem { Amount = amount, Name = string.Join(',', cloneGroceryPieces) });
            }
            else
            {
                groceryItems.Add(new GroceryItem { Amount = "TBD", Name = grocery });
            }
        }
        return groceryItems.ToArray();
    }
}