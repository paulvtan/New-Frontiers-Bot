using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Frontiers_Bot.DataModels
{
    public class ShoppingList
    {
        [JsonProperty(PropertyName = "Id")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "itemName")]
        public string ItemName { get; set; }

        [JsonProperty(PropertyName = "quantity")]
        public int Quantity { get; set; }

        [JsonProperty(PropertyName = "individualPrice")]
        public double IndividualPrice { get; set; }

        [JsonProperty(PropertyName = "sumPrice")]
        public double SumPrice { get; set; }

        [JsonProperty(PropertyName = "strikeOut")]
        public bool StrikeOut { get; set; }

    }
}