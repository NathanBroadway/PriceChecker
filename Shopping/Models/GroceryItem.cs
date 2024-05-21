internal class GroceryItem
{
    public List<StorePrice> Prices { get; set; } = new List<StorePrice>();
    public string Amount { get; set; }
    public string Name { get; set; }
    public string Notes { get; set; }
    public string Coupon { get; set; }

    public override string ToString()
    {
        return $"{Name}: {Amount}: {GetLowestPricedStore()?.Price}{(Notes != "" ? $"\n\t\t{Notes}" : "")}{(Coupon != "" ? $"\n\t\t{Coupon}" : "")}";
    }

    internal StorePrice? GetLowestPricedStore()
    {
        return Prices.Count == 0 ? null : Prices.Where(price => price.Price != null).OrderBy(price => price.Price).FirstOrDefault();
    }

    internal void SetAmount()
    {
        if (Amount == "TBD")
        {
            Console.Write("\nAmount: ");
            Amount = Console.ReadLine();
        }
    }

    internal void SetNotes()
    {
        if (Prices.Count > 0)
        {
            Notes = string.Join(">", Prices.Where(price => price.Price != null).OrderBy(price => price.Price).Select(store => store.StoreName).ToList());
        }
        else
        {
            Console.Write("\nNotes: ");
            Notes = Console.ReadLine();
        }
    }

    internal decimal? SetPrice(string storeName)
    {
        string? priceString = "";
        do
        {
            if (priceString.Contains('/'))
            {
                var pieces = priceString.Split('/');
                if (decimal.TryParse(pieces[0], out var dividened) && decimal.TryParse(pieces[1], out var divisor)) priceString = (dividened / divisor).ToString();
                else priceString = "";
            }
            else
            {
                Console.Write("\nPrice: ");
                priceString = Console.ReadLine()!.ToUpper();
            }
        } while (!decimal.TryParse(priceString, out var price) && priceString != "S");

        var storeItem = new StorePrice
        {
            StoreName = storeName,
            DateRecord = DateTime.Now,
        };

        if (priceString != "S")
        {
            storeItem.Price = decimal.Parse(priceString);
        }

        var existingStorePrice = Prices.Where(price => price.StoreName == storeName).FirstOrDefault();
        if (existingStorePrice != null)
        {
            Prices.Remove(existingStorePrice);
        }

        Prices.Add(storeItem);
        return storeItem.Price;
    }
}