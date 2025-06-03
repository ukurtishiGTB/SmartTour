using Newtonsoft.Json;
using System.Collections.Generic;

namespace SmartTour.Models
{
    public class User
    {
        // This becomes the ArangoDB document’s “_key”
        [JsonProperty("_key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        // An array of “tags” or “preferences”
        [JsonProperty("preferences")]
        public List<string> Preferences { get; set; }
    }
}