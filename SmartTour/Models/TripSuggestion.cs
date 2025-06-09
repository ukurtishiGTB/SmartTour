using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SmartTour.Models
{
    public class TripSuggestion
    {
        [JsonProperty("_key")]
        public string Key { get; set; }
        
        [JsonProperty("_id")]
        public string Id { get; set; }
        
        public string UserKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Place> SuggestedPlaces { get; set; } = new();
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Highlights { get; set; } = new();
        public string ImageUrl { get; set; }
        public bool IsWithinBudget { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } // "Pending", "Accepted", "Rejected"
    }
} 