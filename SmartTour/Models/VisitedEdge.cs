// SmartTour/Data/Models/VisitedEdge.cs
using Newtonsoft.Json;
using System;

namespace SmartTour.Models
{
    public class VisitedEdge
    {
        // Must be formatted as "Users/<userKey>"
        [JsonProperty("_from")]
        public string From { get; set; }

        // Must be formatted as "Places/<placeKey>"
        [JsonProperty("_to")]
        public string To { get; set; }

        [JsonProperty("visitedOn")]
        public DateTime VisitedOn { get; set; }
    }
}