// File: SmartTour/Data/Models/RecommendationResult.cs

using Newtonsoft.Json;

namespace SmartTour.Models
{
    public class RecommendationResult
    {
        // Full ArangoDB _id, e.g. "Places/rome"
        [JsonProperty("placeId")]
        public string PlaceId { get; set; }

        // The place's "name" field (human‐readable)
        [JsonProperty("placeName")]
        public string PlaceName { get; set; }

        // How many times this co‐occurs (used for ranking)
        [JsonProperty("score")]
        public int Score { get; set; }
    }
}