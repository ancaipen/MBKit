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
using MBKit.ECommerce.SalesOrderWS;
using MBKit.ECommerce.SalesOrderLineWS;
using System.Threading.Tasks;

namespace MBKit.ECommerce
{
    class Program
    {

        public static List<Customer> customersNAV = null;
        public static List<SalesOrder> ordersNAV = null;
        public static List<ItemCard> itemsNAV = null;
        public static DateTime lastRunDate;
        public static string Payment_Terms_Code = "PAYPAL";
        public static string Customer_Posting_Group = "DEALER";

        static void Main(string[] args)
        {

            //retrieve all orders and customers for lookups later
            initProcessesAndData().Wait();

            //sync customers from NAV
            //processCustomers().Wait();

            //sync products from NAV
            //processProducts().Wait();

            //sync orders from woo commerce to NAV
            processOrders().Wait();

        }

        public static async Task initProcessesAndData()
        {
            //get and set last run date
            string filePath = ConfigurationManager.AppSettings["local_files"] + @"LastRunDate.txt";
            string dateText = System.IO.File.ReadAllText(filePath);
            DateTime.TryParse(dateText, out lastRunDate);

            //write current datetime back to file
            System.IO.File.WriteAllText(filePath, DateTime.Now.ToString());

            //retrieve all NAV customers
            if (customersNAV == null)
            {
                customersNAV = await getCustomers();
            }

            if (itemsNAV == null)
            {
                itemsNAV = await getItems();
            }

            //retrirve all NAV orders
            if (ordersNAV == null)
            {
                ordersNAV = await getOrders();
            }

        }

        public static async Task writeToLog(string errorMessage, bool writeToConsole = true)
        {
            //get error log
            string filePath = ConfigurationManager.AppSettings["local_files"] + @"ErrorLog.txt";

            //write to console window
            if (writeToConsole)
            {
                Console.WriteLine(errorMessage);
            }

            //write to error file
            string errorFileMessage = DateTime.Now.ToString() + " -- " + errorMessage;
            System.IO.File.AppendAllText(filePath, errorFileMessage);

        }

        public static async Task processOrders()
        {
            //get all orders from woocommerce
            string requestUrl = ConfigurationManager.AppSettings["url"] + "orders";
            string consumerKey = ConfigurationManager.AppSettings["wc_consumerkey"];
            string consumerSecret = ConfigurationManager.AppSettings["wc_concumersecret"];

            List<MBKit.ECommerce.Models.Order> orders = null;

            SalesOrder_Service serviceSalesOrder = new SalesOrder_Service();
            serviceSalesOrder.UseDefaultCredentials = true;

            SalesOrderLine_Service serviceSalesOrderLine = new SalesOrderLine_Service();
            serviceSalesOrderLine.UseDefaultCredentials = true;

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
                string errMsg = "processOrders() | WooCommerce Order Retrieval | ERROR: " + ex.Message;
                writeToLog(errMsg).Wait();
            }

            if (orders != null)
            {
                //remove failed orders
                orders = orders.Where(x => x.status != null && x.status.ToLower().Trim() != "failed").ToList();

                //loop through orders and 
                if (orders != null && orders.Count > 0)
                {
                                        
                    foreach (var order in orders)
                    {
                        //check to make sure order is not already added in NAV
                        var orderFound = ordersNAV.Where(x => x.Customer_Order_No != null && x.Customer_Order_No == order.order_key).FirstOrDefault();

                        if (orderFound == null)
                        {

                            try
                            {
                                //retrieve customer
                                Customer cust = customersNAV.Where(x => x.ExternalID != null && x.ExternalID == order.customer_id).FirstOrDefault();

                                if (cust != null)
                                {

                                    DateTime _orderDate;
                                    DateTime.TryParse(order.date_created, out _orderDate);

                                    SalesOrder salesOrder = ordersNAV.Where(x => x.External_Document_No != null && x.External_Document_No == order.id).FirstOrDefault();

                                    if (salesOrder == null)
                                    {
                                        //create new sales order
                                        serviceSalesOrder.Create(ref salesOrder);
                                    }
                                                                                
                                    salesOrder.Order_Date = _orderDate;
                                    salesOrder.External_Document_No = order.id;
                                    salesOrder.Payment_Terms_Code = Payment_Terms_Code;
                                    
                                    if (cust != null)
                                    {
                                        salesOrder.Bill_to_Customer_No = cust.No;
                                        salesOrder.Sell_to_Customer_No = cust.No;
                                    }

                                    serviceSalesOrder.Update(ref salesOrder);

                                    //assign line items
                                    if (salesOrder != null && order.line_items != null && order.line_items.Count > 0)
                                    {

                                        //create sales lines for update below
                                        int salesLineCount = order.line_items.Count;
                                        salesOrder.SalesLines = new Sales_Order_Line[salesLineCount];
                                        
                                        for (int idx = 0; idx < salesLineCount; idx++)
                                        {
                                            salesOrder.SalesLines[idx] = new Sales_Order_Line();
                                            serviceSalesOrder.Update(ref salesOrder);

                                            var salesOrderLine = salesOrder.SalesLines[idx];
                                            var line_item = order.line_items[idx];

                                            salesOrderLine.No = line_item.sku;
                                            salesOrderLine.Type = SalesOrderWS.Type.Item;
                                            salesOrderLine.Document_No = salesOrder.No;
                                            salesOrderLine.Quantity = line_item.quantity;

                                            salesOrderLine.Unit_Price = line_item.price;
                                            salesOrderLine.Unit_PriceSpecified = true;

                                            salesOrderLine.Line_Amount = line_item.total;
                                            salesOrderLine.Line_AmountSpecified = true;

                                            decimal requiredLengthMM = 0;

                                            if (line_item.meta_data != null)
                                            {
                                                foreach (var meta_data in line_item.meta_data)
                                                {
                                                    if (meta_data.key == "Required Length (mm)")
                                                    {
                                                        requiredLengthMM = Convert.ToDecimal(meta_data.value);
                                                    }
                                                }
                                                
                                            }
                                            
                                            serviceSalesOrder.Update(ref salesOrder);

                                        }
                                        
                                    }
                                }
                                
                            }
                            catch (Exception ex)
                            {   
                                string errMsg = "processOrders() | NAV Order Create | ERROR: " + ex.Message;
                                writeToLog(errMsg).Wait();
                            }

                        }

                    }
                }
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
                string errMsg = "processCustomers() | WooCommerce Customer Retrieval | ERROR: " + ex.Message;
                writeToLog(errMsg).Wait();
            }

            if (customersWoo != null && customersNAV != null)
            {
                //create new customers
                foreach (var customerWoo in customersWoo)
                {
                    try
                    {

                        //check to make sure username is not already found
                        Customer cust = customersNAV.Where(x => x.ExternalID != null && x.ExternalID == customerWoo.id).FirstOrDefault();

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
                        cust.ExternalID = customerWoo.id;
                        cust.Search_Name = customerWoo.username;
                        cust.E_Mail = customerWoo.email;
                        cust.Customer_Posting_Group = Customer_Posting_Group; //this is required for sales orders
                        cust.Payment_Terms_Code = Payment_Terms_Code;

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

                        if (string.IsNullOrEmpty(cust.No))
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
                        string errMsg = "processCustomers() | NAV Customer Upsert | ERROR: " + ex.Message;
                        writeToLog(errMsg).Wait();
                    }
                }

                //refresh customer info
                customersNAV = await getCustomers();

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

                try
                {

                    if (pageCount == pageCountLimit)
                    {
                        break;
                    }

                    bookmarkKey = results.Last().Key;
                    customerList.AddRange(results);
                    results = service.ReadMultiple(new Customer_Filter[] { }, bookmarkKey, fetchSize);
                    
                    Console.WriteLine("getCustomers() retriving customers: " + pageCount.ToString());
                }
                catch (Exception ex)
                {
                    string errMsg = "ERROR: " + pageCount.ToString() + " getCustomers() " + ex.Message;
                    writeToLog(errMsg).Wait();
                }             

                pageCount++;
            }

            return customerList;

        }

        public static async Task<List<SalesOrder>> getOrders()
        {

            SalesOrder_Service service = new SalesOrder_Service();
            service.UseDefaultCredentials = true;

            int pageCountLimit = 5000;
            int pageCount = 0;
            const int fetchSize = 20;
            string bookmarkKey = null;
            List<SalesOrder> orderList = new List<SalesOrder>();

            // Reads NAV.ItemCard data in pages of 10.
            SalesOrder[] results = service.ReadMultiple(new SalesOrder_Filter[] { }, bookmarkKey, fetchSize);

            while (results.Length > 0)
            {
                try
                {

                    if (pageCount == pageCountLimit)
                    {
                        break;
                    }

                    bookmarkKey = results.Last().Key;
                    orderList.AddRange(results);
                    results = service.ReadMultiple(new SalesOrder_Filter[] { }, bookmarkKey, fetchSize);

                    Console.WriteLine("getOrders() retriving sales orders: " + pageCount.ToString());

                }
                catch (Exception ex)
                {
                    string errMsg = "ERROR: " + pageCount.ToString() + " getOrders() " + ex.Message;
                    writeToLog(errMsg).Wait();
                }
                
                pageCount++;
            }

            return orderList;

        }

        public static async Task<List<ItemCard>> getItems()
        {

            ItemCard_Service service = new ItemCard_Service();
            service.UseDefaultCredentials = true;

            int pageCountLimit = 1000;
            int pageCount = 0;
            const int fetchSize = 20;
            string bookmarkKey = null;
            List<ItemCard> itemList = new List<ItemCard>();

            // Reads NAV.ItemCard data in pages of 10.
            ItemCard[] results = service.ReadMultiple(new ItemCard_Filter[] { }, bookmarkKey, fetchSize);

            while (results.Length > 0)
            {
                try
                {

                    if (pageCount == pageCountLimit)
                    {
                        break;
                    }

                    bookmarkKey = results.Last().Key;
                    itemList.AddRange(results);
                    results = service.ReadMultiple(new ItemCard_Filter[] { }, bookmarkKey, fetchSize);
                                        
                    Console.WriteLine("getItems() retriving inventory items: " + pageCount.ToString());
                }
                catch(Exception ex) 
                {
                    string errMsg = "ERROR: " + pageCount.ToString() + " getItems() " + ex.Message;
                    writeToLog(errMsg).Wait();
                    break;
                }

                pageCount++;
            }

            return itemList;

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

            string filePath = ConfigurationManager.AppSettings["local_files"] + @"product.txt";
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
