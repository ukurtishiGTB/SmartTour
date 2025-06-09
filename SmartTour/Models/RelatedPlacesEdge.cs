// SmartTour/Data/Models/RelatedPlacesEdge.cs
using Newtonsoft.Json;

namespace SmartTour.Models
{
    public class RelatedPlacesEdge
    {
        [JsonProperty("_from")]
        public string From { get; set; }    // e.g. "Places/paris"

        [JsonProperty("_to")]
        public string To { get; set; }      // e.g. "Places/rome"

        [JsonProperty("weight")]
        public double Weight { get; set; }
    }
}