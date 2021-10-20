using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBKit.ECommerce.Models
{
    public class Customer
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("date_created")]
        public string date_created { get; set; }

        [JsonProperty("date_modified")]
        public string date_modified { get; set; }

        [JsonProperty("first_name")]
        public string first_name { get; set; }

        [JsonProperty("last_name")]
        public string last_name { get; set; }

        [JsonProperty("username")]
        public string username { get; set; }

        [JsonProperty("role")]
        public string role { get; set; }

        [JsonProperty("email")]
        public string email { get; set; }

        [JsonProperty("billing")]
        public Address billing { get; set; }

        [JsonProperty("shipping")]
        public Address shipping { get; set; }

    }

    public class Address
    {

        [JsonProperty("first_name")]
        public string first_name { get; set; }

        [JsonProperty("last_name")]
        public string last_name { get; set; }

        [JsonProperty("company")]
        public string company { get; set; }

        [JsonProperty("address_1")]
        public string address_1 { get; set; }

        [JsonProperty("address_2")]
        public string address_2 { get; set; }

        [JsonProperty("city")]
        public string city { get; set; }

        [JsonProperty("postcode")]
        public string postcode { get; set; }

        [JsonProperty("country")]
        public string country { get; set; }

        [JsonProperty("state")]
        public string state { get; set; }

        [JsonProperty("email")]
        public string email { get; set; }

        [JsonProperty("phone")]
        public string phone { get; set; }

    }

}
