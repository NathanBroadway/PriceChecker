using System.Diagnostics;

namespace Shopping.Models
{
    internal class Store
    {
        public string Name { get; set; }
        public decimal Total { get; set; } = 0;
        public List<GroceryItem> Items { get; } = new List<GroceryItem>();
        public string StoreUrl { get; set; }
        public string SeperateCharacter { get; set; }
        public char? SubInitial { get; set; }
        public Store()
        {

        }
        public Store(string name)
        {
            Name = name;
        }

        public override string ToString()
        {
            var finalString = "\n\n\n";
            finalString += Name + ":";
            foreach (var groceryItem in Items)
            {
                finalString += "\n\t" + groceryItem.ToString() + '\n';
            }
            return finalString;
        }

        public void OpenStore(string grocery, decimal? cheapestPrice)
        {
            var process = GetNewProcess();
            var tempUrl = StoreUrl.Replace("{{SEPERATECHAR}}", string.Join(SeperateCharacter, grocery.Split(' ')));
            if (tempUrl.Contains("CHEAPESTPRICE"))
            {
                tempUrl = tempUrl.Replace("{{CHEAPESTPRICE}}", cheapestPrice.ToString());
            }
            process.StartInfo.Arguments = tempUrl;
            process.Start();
        }
        private static Process GetNewProcess()
        {
            var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = "chrome";
            return process;
        }

        public char Intial()
        {
            return SubInitial ?? Name[0];
        }
    }
}
