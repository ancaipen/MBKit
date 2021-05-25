using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using MBKit.ECommerce.ItemCardWS;
using System.Threading.Tasks;

namespace MBKit.ECommerce
{
    class Program
    {
        static void Main(string[] args)
        {
            getMBProducts();
        }

        public static async void getMBProducts()
        {

            ItemCard_Service service = new ItemCard_Service();
            service.UseDefaultCredentials = true;

            int pageCountLimit = 1;
            int pageCount = 0;
            const int fetchSize = 10;
            string bookmarkKey = null;
            List<ItemCard> itemCardList = new List<ItemCard>();

            // Reads NAV.ItemCard data in pages of 10.
            ItemCard[] results = service.ReadMultiple(new ItemCard_Filter[] { }, bookmarkKey, fetchSize);
            
            while (results.Length > 0)
            {
                bookmarkKey = results.Last().Key;
                itemCardList.AddRange(results);
                results = service.ReadMultiple(new ItemCard_Filter[] { }, bookmarkKey, fetchSize);

                if (pageCount == pageCountLimit)
                {
                    break;
                }

                pageCount++;
            }

            // Prints the collected data.  
            foreach (ItemCard itemCard in itemCardList)
            {
                if (!string.IsNullOrEmpty(itemCard.Description))
                {
                    //create product
                    createProduct(itemCard.Description, itemCard.Description, 0, itemCard.Description, itemCard.Description).Wait();
                }
            }

        }

        public static async Task createProduct(string sku, string product_name, decimal regular_price, string description, string short_description)
        {

            string requestUrl = "https://www.mbkit.com/products/wp-json/wc/v3/products";//ConfigurationManager.AppSettings["url"] + "/wp-json/wc/v3/products";
            string consumerKey = "ck_4fc7f819bcbd9b13fd7a7df3b4ba7b255856d20a"; //ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = "cs_436945dab87715a5b9ba80ec4736b6c2b946780f"; //ConfigurationManager.AppSettings["wc_concumersecret"];

            string filePath = @"C:\Users\aaroncaipen\source\repos\MBKit\MBKit.ECommerce\Files\product.txt";
            string jsonValues = System.IO.File.ReadAllText(filePath);

            //replace values
            jsonValues.Replace("[sku]", sku);
            jsonValues.Replace("[product_name]", product_name);
            jsonValues.Replace("[regular_price]", regular_price.ToString());
            jsonValues.Replace("[description]", description);

            var content = new StringContent(jsonValues, Encoding.UTF8, "application/json");

            try
            {

                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/* ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br ");

                var byteArray = Encoding.ASCII.GetBytes(consumerKey + ":" + consumerSecret);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await httpClient.PostAsync(requestUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }

        }
    }
}
