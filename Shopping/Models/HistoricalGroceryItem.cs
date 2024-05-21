internal class HistoricalGroceryItem : GroceryItem
{
    public string StoreName { get; set; }
    public DateTime DatePrice { get; set; } = DateTime.UtcNow;
}