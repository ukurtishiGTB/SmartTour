using System.Collections.Generic;
using SmartTour.Models;

namespace SmartTour.ViewModels
{
    public class MyTripsViewModel
    {
        public IEnumerable<Trip> UpcomingTrips { get; set; }
        public IEnumerable<Trip> PastTrips { get; set; }
    }
} 