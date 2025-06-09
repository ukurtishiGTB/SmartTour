using SmartTour.Models;

namespace SmartTour.ViewModels;

public class TripPlanningViewModel
{
    // User Information
    public string UserKey { get; set; }
    
    // Initial Trip Planning Form
    public string? DestinationPreference { get; set; }  // Can be null for "surprise me"
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationInDays { get; set; }
    public bool IsSolo { get; set; }
    public List<string>? ParticipantKeys { get; set; } = new();  // Friend keys who will join
    public List<string> ActivityTypes { get; set; } = new();
    public decimal Budget { get; set; }
    
    // Additional preferences that might override user's default preferences
    public string? PreferredTravelType { get; set; }  // Nature, City, Adventure, Relax
    public List<string> SpecificInterests { get; set; } = new();
}

public class TripSuggestionViewModel
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<Place> SuggestedPlaces { get; set; } = new();
    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<string> Highlights { get; set; } = new();
    public string ImageUrl { get; set; }
    public bool IsWithinBudget { get; set; }
} 