using Newtonsoft.Json;
using System.Collections.Generic;

namespace SmartTour.Models
{
    public class Place
    {
        [JsonProperty("_key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        // e.g. “city”, “landmark”, etc.
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("coordinates")]
        public Coordinates Coordinates { get; set; }
    }

    public class Coordinates
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }
}