using Shopping.Models;

namespace Shopping.Extensions
{
    internal static class ListExtensions
    {
        internal static string GetStoreName(this List<Store> stores, ConsoleKey storeKey)
        {
            return stores.FirstOrDefault(store => store.Intial().ToString().ToLower() == storeKey.ToString().ToLower())?.Name;
        }

        internal static void AddGroceryItemToSelectedStore(this List<Store> stores, string storeName, GroceryItem groceryItem)
        {
            var store = stores.First(store => store.Name == storeName);
            AddToList(groceryItem, store);
        }

        private static void AddToList(GroceryItem groceryItem, Store store)
        {
            store.Items.Add(groceryItem);
            store.Total += groceryItem.GetLowestPricedStore().Price.Value;
        }

        internal static void DisplayStoreChoices(this List<Store> stores)
        {
            foreach (Store store in stores)
            {
                Console.WriteLine($"[{store.Intial()}] {store.Name}");
            }
        }

        internal static void QuestionList(this GroceryItem[] groceries)
        {
            var dislikes = File.ReadAllLines(@"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\Dislikes.txt");
            var dislikesInGroceries = groceries.CheckGroceriesAgainstThis(dislikes);
            if (dislikesInGroceries.Any())
            {
                Console.Write($"Warning This List Contains Disliked Foods\n" + string.Join("\n", dislikesInGroceries) + "Hit Escape to cancel list.");
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    throw new Exception();
                }
            }

            var unfindables = File.ReadAllLines(@"C:\Users\ngbro\source\repos\Shopping\Shopping\CurrentFiles\Unfindables.txt");
            var unfindableInGroceries = groceries.CheckGroceriesAgainstThis(unfindables);
            if (dislikesInGroceries.Any())
            {
                Console.Write($"Warning This List Contains Unfindable Foods\n" + string.Join("\n", unfindables) + "Hit Escape to cancel list.");
                if (Console.ReadKey().Key == ConsoleKey.Escape)
                {
                    throw new Exception();
                }
            }
        }

        private static List<string> CheckGroceriesAgainstThis(this GroceryItem[] groceries, string[] dislikes)
        {
            var dislikesInGroceries = new List<string>();
            foreach (var grocery in groceries)
            {
                foreach (var dislikedFood in dislikes)
                {
                    if (grocery.Name.ToLower().Contains(dislikedFood.ToLower()))
                    {
                        dislikesInGroceries.Add(grocery.Name);
                    }
                }
            }

            return dislikesInGroceries;
        }
    }
}
