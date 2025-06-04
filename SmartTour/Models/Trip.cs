// SmartTour/Data/Models/Trip.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SmartTour.Models
{
    public class Trip
    {
        [JsonProperty("_key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("user_key")]
        public string UserKey { get; set; }

        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }

        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }

        [JsonProperty("places")]
        public List<Place> Places { get; set; } = new List<Place>();

        [JsonProperty("status")]
        public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Cancelled

        [JsonProperty("rating")]
        public int? Rating { get; set; }

        [JsonProperty("review")]
        public string Review { get; set; }

        [JsonProperty("budget")]
        public decimal Budget { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string Id => Key; // For convenience in views
    }
}