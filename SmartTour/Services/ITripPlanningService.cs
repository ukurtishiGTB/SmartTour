using SmartTour.Models;
using SmartTour.ViewModels;

namespace SmartTour.Services;

public interface ITripPlanningService
{
    Task<TripSuggestionViewModel> GenerateTripSuggestionAsync(
        string userKey,
        TripPlanningViewModel planningDetails);
    
    Task<Trip> CreateTripFromSuggestionAsync(
        string userKey,
        TripSuggestionViewModel suggestion,
        bool acceptSuggestion);
        
    Task<List<string>> GetPopularDestinationsAsync(string travelType);
    Task<List<Place>> GeneratePlacesAsync(string destination, List<string> interests, decimal budget);
    
    // User preferences management
    Task<TravelPreferences> GetUserPreferencesAsync(string userKey);
    Task UpdateUserPreferencesAsync(string userKey, TravelPreferences preferences);
} 