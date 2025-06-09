using SmartTour.Models;
using SmartTour.ViewModels;

namespace SmartTour.Services;

public class TripPlanningService
{
    private readonly TripService _tripService;
    private readonly RecommendationService _recommendationService;
    private readonly UserService _userService;

    public TripPlanningService(
        TripService tripService,
        RecommendationService recommendationService,
        UserService userService)
    {
        _tripService = tripService;
        _recommendationService = recommendationService;
        _userService = userService;
    }

    public async Task<TravelPreferences> GetUserPreferencesAsync(string userKey)
    {
        // Get user from database
        var user = await _userService.GetByKeyAsync(userKey);
        if (user == null)
        {
            // Return default preferences if user not found
            return new TravelPreferences
            {
                UserKey = userKey,
                TravelType = "Adventure",
                Interests = new List<string> { "Nature", "Culture" }
            };
        }

        // Map user preferences to TravelPreferences model
        return new TravelPreferences
        {
            UserKey = userKey,
            TravelType = user.TravelStyle ?? "Adventure",
            Interests = user.Preferences ?? new List<string> { "Nature", "Culture" }
        };
    }

    public async Task<TripSuggestionViewModel> GenerateTripSuggestionAsync(
        string userKey,
        TripPlanningViewModel planningDetails)
    {
        List<Place> recommendations;

        // Get user's default preferences if not specified in planning details
        var userPreferences = await GetUserPreferencesAsync(userKey);
        
        // Use provided preferences or fall back to defaults
        var travelType = !string.IsNullOrEmpty(planningDetails.PreferredTravelType) 
            ? planningDetails.PreferredTravelType 
            : userPreferences?.TravelType ?? "Adventure";
            
        var interests = planningDetails.SpecificInterests?.Any() == true 
            ? planningDetails.SpecificInterests 
            : userPreferences?.Interests ?? new List<string> { "Nature", "Culture" };

        // If a specific destination is provided, use RecommendNearbyPlacesAsync
        if (!string.IsNullOrEmpty(planningDetails.DestinationPreference))
        {
            recommendations = await _recommendationService.RecommendNearbyPlacesAsync(
                planningDetails.DestinationPreference
            );
        }
        // Otherwise, generate a personalized trip
        else
        {
            // Pass the resolved preferences to the recommendation service
            recommendations = await _recommendationService.GeneratePersonalizedTripAsync(
                userKey,
                (planningDetails.EndDate - planningDetails.StartDate)?.Days ?? 7,
                5,
                travelType,
                interests
            );
        }

        if (!recommendations.Any())
        {
            throw new InvalidOperationException("No recommendations found for the given criteria.");
        }

        var destinationName = !string.IsNullOrEmpty(planningDetails.DestinationPreference)
            ? planningDetails.DestinationPreference
            : recommendations.First().City;

        // Create a trip suggestion
        return new TripSuggestionViewModel
        {
            Name = $"Trip to {destinationName}",
            Description = "A personalized trip based on your preferences",
            SuggestedPlaces = recommendations,
            Budget = planningDetails.Budget,
            StartDate = planningDetails.StartDate ?? DateTime.Now.AddDays(30),
            EndDate = planningDetails.EndDate ?? DateTime.Now.AddDays(37),
            Highlights = recommendations.Select(p => p.Description).Take(5).ToList(),
            ImageUrl = recommendations.First().ImageUrl,
            IsWithinBudget = true // You should calculate this based on actual costs
        };
    }

    public async Task<Trip> CreateTripFromSuggestionAsync(
        string userKey,
        TripSuggestionViewModel suggestion,
        bool acceptSuggestion)
    {
        var trip = new Trip
        {
            Name = suggestion.Name,
            Description = suggestion.Description,
            UserKey = userKey,
            StartDate = suggestion.StartDate,
            EndDate = suggestion.EndDate,
            Places = suggestion.SuggestedPlaces,
            Status = "Planned",
            Budget = suggestion.Budget,
            ImageUrl = suggestion.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Save the trip to your database and get the ID
        var tripId = await _tripService.CreateAsync(trip);
        
        // Extract the key from the full ID (format: "Trips/key")
        trip.Key = tripId.Split('/').Last();

        return trip;
    }
} 