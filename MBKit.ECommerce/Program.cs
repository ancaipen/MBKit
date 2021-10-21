using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using MBKit.ECommerce.ItemCardWS;
using MBKit.ECommerce.CustomerWS;
using System.Threading.Tasks;

namespace MBKit.ECommerce
{
    class Program
    {
        static void Main(string[] args)
        {

            //sync customers from NAV
            processCustomers().Wait();

            //sync products from NAV
            //processProducts().Wait();

            //sync orders from woo commerce to NAV
            processOrders().Wait();

        }

        public static async Task processOrders()
        {
            //get all orders from woocommerce
            string requestUrl = ConfigurationManager.AppSettings["url"] + "orders";
            string consumerKey = ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = ConfigurationManager.AppSettings["wc_concumersecret"];

            List<MBKit.ECommerce.Models.Order> orders = null;

            try
            {

                //retrieve all orders
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/* ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br ");

                var byteArray = Encoding.ASCII.GetBytes(consumerKey + ":" + consumerSecret);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await httpClient.GetAsync(requestUrl);
                var responseString = await response.Content.ReadAsStringAsync();

                //create list of objects from response
                orders = JsonConvert.DeserializeObject<List<MBKit.ECommerce.Models.Order>>(responseString);

            }
            catch (Exception ex)
            {
                Console.WriteLine("processOrders() ERROR: " + ex.Message);
            }

            if (orders != null)
            {
                //remove failed orders
                orders = orders.Where(x => x.status != null && x.status.ToLower().Trim() != "failed").ToList();

                //loop through orders and 

            }

        }

        public static async Task processCustomers()
        {
            //get all orders from woocommerce
            string requestUrl = ConfigurationManager.AppSettings["url"] + "customers";
            string consumerKey = ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = ConfigurationManager.AppSettings["wc_concumersecret"];

            Customer_Service service = new Customer_Service();
            service.UseDefaultCredentials = true;

            List<MBKit.ECommerce.Models.Customer> customersWoo = null;

            try
            {

                //retrieve all customers
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/* ");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br ");

                var byteArray = Encoding.ASCII.GetBytes(consumerKey + ":" + consumerSecret);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var response = await httpClient.GetAsync(requestUrl);
                var responseString = await response.Content.ReadAsStringAsync();

                //create list of objects from response
                customersWoo = JsonConvert.DeserializeObject<List<MBKit.ECommerce.Models.Customer>>(responseString);

            }
            catch (Exception ex)
            {
                Console.WriteLine("processCustomers() ERROR: " + ex.Message);
            }

            //retrieve all NAV customers
            List<Customer> customersNAV = await getCustomers();

            if (customersWoo != null && customersNAV != null)
            {
                //create new customers
                foreach (var customerWoo in customersWoo)
                {
                    try
                    {

                        //check to make sure username is not already found
                        Customer cust = customersNAV.Where(x => x.ExternalID != null && x.ExternalID == customerWoo.username).FirstOrDefault();

                        //check by email
                        if (cust == null && !string.IsNullOrEmpty(customerWoo.email))
                        {
                            cust = customersNAV.Where(x => x.E_Mail != null && x.E_Mail == customerWoo.email).FirstOrDefault();
                        }

                        //create customer if not found
                        if (cust == null)
                        {
                            cust = new Customer();
                        }

                        cust.Name = customerWoo.first_name + " " + customerWoo.last_name;
                        cust.ExternalID = customerWoo.username;
                        cust.E_Mail = customerWoo.email;

                        if (customerWoo.billing != null)
                        {

                            cust.Address = customerWoo.billing.address_1;
                            cust.Address_2 = customerWoo.billing.address_2;
                            cust.Post_Code = customerWoo.billing.postcode;
                            cust.City = customerWoo.billing.city;
                            cust.County = customerWoo.billing.state;

                            if (!string.IsNullOrEmpty(customerWoo.billing.country) && customerWoo.billing.country == "US")
                            {
                                cust.Country_Region_Code = "USA";
                            }

                            cust.Phone_No = customerWoo.billing.phone;
                        }

                        if (cust == null)
                        {
                            service.Create(ref cust);
                        }
                        else
                        {
                            service.Update(ref cust);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("processCustomers() ERROR: " + ex.Message);
                    }
                }
            }

        }

        public static async Task<List<Customer>> getCustomers()
        {
                        
            Customer_Service service = new Customer_Service();
            service.UseDefaultCredentials = true;

            int pageCountLimit = 5000;
            int pageCount = 0;
            const int fetchSize = 20;
            string bookmarkKey = null;
            List<Customer> customerList = new List<Customer>();

            // Reads NAV.ItemCard data in pages of 10.
            Customer[] results = service.ReadMultiple(new Customer_Filter[] { }, bookmarkKey, fetchSize);

            while (results.Length > 0)
            {
                bookmarkKey = results.Last().Key;
                customerList.AddRange(results);
                results = service.ReadMultiple(new Customer_Filter[] { }, bookmarkKey, fetchSize);

                if (pageCount == pageCountLimit)
                {
                    break;
                }

                Console.WriteLine("getCustomers() retriving customers: " + pageCount.ToString());

                pageCount++;
            }

            return customerList;

        }

        public static async Task<List<Customer>> getOrders()
        {

            Customer_Service service = new Customer_Service();
            service.UseDefaultCredentials = true;

            int pageCountLimit = 5000;
            int pageCount = 0;
            const int fetchSize = 20;
            string bookmarkKey = null;
            List<Customer> customerList = new List<Customer>();

            // Reads NAV.ItemCard data in pages of 10.
            Customer[] results = service.ReadMultiple(new Customer_Filter[] { }, bookmarkKey, fetchSize);

            while (results.Length > 0)
            {
                bookmarkKey = results.Last().Key;
                customerList.AddRange(results);
                results = service.ReadMultiple(new Customer_Filter[] { }, bookmarkKey, fetchSize);

                if (pageCount == pageCountLimit)
                {
                    break;
                }

                Console.WriteLine("getCustomers() retriving customers: " + pageCount.ToString());

                pageCount++;
            }

            return customerList;

        }

        public static async Task processProducts()
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

            string requestUrl = ConfigurationManager.AppSettings["url"] + "/products";
            string consumerKey = ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = ConfigurationManager.AppSettings["wc_concumersecret"];

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
