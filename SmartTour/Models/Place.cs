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

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("continent")]
        public string Continent { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("image")]
        public string ImageUrl { get; set; }
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