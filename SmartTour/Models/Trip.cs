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

        // Must match a User’s `_key` in the Users collection
        [JsonProperty("userId")]
        public string UserId { get; set; }

        // An array of Place `_key` values
        [JsonProperty("places")]
        public List<string> Places { get; set; }

        [JsonProperty("startDate")]
        public DateTime StartDate { get; set; }

        [JsonProperty("endDate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
        
        [JsonIgnore]
        public string PlacesCsv
        {
            get => (Places != null && Places.Any())
                ? string.Join(", ", Places)
                : string.Empty;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Places = new List<string>();
                }
                else
                {
                    Places = value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
            }
        }
    }
}