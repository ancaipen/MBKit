using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using MBKit.ECommerce.ItemCardWS;

namespace MBKit.ECommerce
{
    class Program
    {
        static void Main(string[] args)
        {
            getMBProducts();
        }

        public static void getMBProducts()
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
                Console.WriteLine(itemCard.Serial_Nos);
            }

        }

        public static async void createProduct(string product_name, string product_type, decimal regular_price, string description, string short_description)
        {

            string requestUrl = ConfigurationManager.AppSettings["url"] + "/wp-json/wc/v3/products";
            string consumerKey = ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = ConfigurationManager.AppSettings["wc_concumersecret"];

            var values = new Dictionary<string, string>
            {
                { "name", product_name },
                { "type", product_type },
                { "regular_price", regular_price.ToString() },
                { "description", description },
                { "short_description", short_description }
            };

            string entryPut = JsonConvert.SerializeObject(values);

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json ");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/* ");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br ");

            var byteArray = Encoding.ASCII.GetBytes(consumerKey + ":" + consumerSecret);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokenOAuth.Token);

            var content = new FormUrlEncodedContent(values);
            var response = await httpClient.PostAsync(requestUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

        }
    }
}
