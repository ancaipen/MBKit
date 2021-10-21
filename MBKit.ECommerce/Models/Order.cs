using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBKit.ECommerce.Models
{
    public class Order
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("date_created")]
        public string date_created { get; set; }

        [JsonProperty("date_modified")]
        public string date_modified { get; set; }

        [JsonProperty("shipping_total")]
        public decimal shipping_total { get; set; }

        [JsonProperty("total")]
        public decimal total { get; set; }

        [JsonProperty("total_tax")]
        public decimal total_tax { get; set; }

        [JsonProperty("customer_id")]
        public string customer_id { get; set; }

        [JsonProperty("order_key")]
        public string order_key { get; set; }

        [JsonProperty("line_items")]
        public List<OrderLine> line_items { get; set; }

        [JsonProperty("billing")]
        public Address billing { get; set; }

        [JsonProperty("shipping")]
        public Address shipping { get; set; }

    }

    public class OrderLine
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("name")]
        public string name { get; set; }

        [JsonProperty("product_id")]
        public string product_id { get; set; }

        [JsonProperty("sku")]
        public string sku { get; set; }

        [JsonProperty("quantity")]
        public decimal quantity { get; set; }

        [JsonProperty("total")]
        public decimal total { get; set; }

        [JsonProperty("total_tax")]
        public decimal total_tax { get; set; }

        [JsonProperty("price")]
        public decimal price { get; set; }

        [JsonProperty("meta_data")]
        public List<OrderLineMetaData> meta_data { get; set; }
               

    }

    public class OrderLineMetaData
    {
        [JsonProperty("key")]
        public string key { get; set; }

        [JsonProperty("value")]
        public object value { get; set; }
    }

    public class OrderLineKeyValue
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("key")]
        public string key { get; set; }

        [JsonProperty("value")]
        public string value { get; set; }

        [JsonProperty("display_key")]
        public string display_key { get; set; }

        [JsonProperty("display_value")]
        public decimal display_value { get; set; }

    }



}
