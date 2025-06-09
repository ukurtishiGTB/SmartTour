using Newtonsoft.Json;

namespace SmartTour.Models;

public class TravelPreferences
{
    [JsonProperty("_key")]
    public string Key { get; set; }
    
    [JsonProperty("user_key")]
    public string UserKey { get; set; }
    
    [JsonProperty("budget_range")]
    public string BudgetRange { get; set; }  // "Budget", "Moderate", "Luxury"
    
    [JsonProperty("travel_type")]
    public string TravelType { get; set; }  // "Nature", "City", "Adventure", "Relax"
    
    [JsonProperty("interests")]
    public List<string> Interests { get; set; } = new();  // "Beaches", "Mountains", "History", etc.
    
    public string Id => Key;
} 