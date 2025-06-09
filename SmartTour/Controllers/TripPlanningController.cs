using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using SmartTour.ViewModels;
using System;
using System.Threading.Tasks;

namespace SmartTour.Controllers;

[Authorize]
public class TripPlanningController : Controller
{
    private readonly TripPlanningService _tripPlanningService;
    private readonly UserService _userService;
    private readonly TripSuggestionService _tripSuggestionService;

    public TripPlanningController(
        TripPlanningService tripPlanningService,
        UserService userService,
        TripSuggestionService tripSuggestionService)
    {
        _tripPlanningService = tripPlanningService;
        _userService = userService;
        _tripSuggestionService = tripSuggestionService;
    }

    [HttpGet]
    public async Task<IActionResult> PlanTrip([FromQuery] string userKey)
    {
        if (string.IsNullOrEmpty(userKey))
        {
            return BadRequest("User key is required");
        }

        var user = await _userService.GetByKeyAsync(userKey);
        if (user == null)
        {
            return NotFound("User not found");
        }

        var preferences = await _tripPlanningService.GetUserPreferencesAsync(userKey);
        
        var model = new TripPlanningViewModel
        {
            UserKey = userKey,
            PreferredTravelType = preferences?.TravelType,
            SpecificInterests = preferences?.Interests ?? new List<string>()
        };
        
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> PlanTrip(TripPlanningViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (string.IsNullOrEmpty(model.UserKey))
        {
            return BadRequest("User key is required");
        }

        var user = await _userService.GetByKeyAsync(model.UserKey);
        if (user == null)
        {
            return NotFound("User not found");
        }

        try
        {
            var suggestion = await _tripPlanningService.GenerateTripSuggestionAsync(model.UserKey, model);
            
            // Create a TripSuggestion entity and save it to ArangoDB
            var tripSuggestion = new TripSuggestion
            {
                Key = Guid.NewGuid().ToString(),
                UserKey = model.UserKey,
                Name = suggestion.Name,
                Description = suggestion.Description,
                SuggestedPlaces = suggestion.SuggestedPlaces,
                Budget = suggestion.Budget,
                StartDate = suggestion.StartDate,
                EndDate = suggestion.EndDate,
                Highlights = suggestion.Highlights,
                ImageUrl = suggestion.ImageUrl,
                IsWithinBudget = suggestion.IsWithinBudget,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            await _tripSuggestionService.CreateAsync(tripSuggestion);
            
            return RedirectToAction(nameof(ReviewSuggestion), new { key = tripSuggestion.Key });
        }
        catch (InvalidOperationException)
        {
            ViewBag.UserKey = model.UserKey;
            return View("NoRecommendations");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ReviewSuggestion(string key)
    {
        var suggestion = await _tripSuggestionService.GetByKeyAsync(key);
        if (suggestion == null)
            return RedirectToAction("Index", "Home");

        var user = await _userService.GetByKeyAsync(suggestion.UserKey);
        if (user == null)
        {
            return NotFound("User not found");
        }

        return View(suggestion);
    }

    [HttpPost]
    public async Task<IActionResult> AcceptSuggestion(string key)
    {
        var suggestion = await _tripSuggestionService.GetByKeyAsync(key);
        if (suggestion == null)
            return RedirectToAction("Index", "Home");

        suggestion.Status = "Accepted";
        await _tripSuggestionService.UpdateAsync(suggestion.Key, suggestion);

        // Map TripSuggestion to TripSuggestionViewModel
        var suggestionViewModel = new TripSuggestionViewModel
        {
            Name = suggestion.Name,
            Description = suggestion.Description,
            SuggestedPlaces = suggestion.SuggestedPlaces,
            Budget = suggestion.Budget,
            StartDate = suggestion.StartDate,
            EndDate = suggestion.EndDate,
            Highlights = suggestion.Highlights,
            ImageUrl = suggestion.ImageUrl,
            IsWithinBudget = suggestion.IsWithinBudget
        };

        try
        {
            var trip = await _tripPlanningService.CreateTripFromSuggestionAsync(suggestion.UserKey, suggestionViewModel, true);
            if (string.IsNullOrEmpty(trip.Key))
            {
                // If trip creation failed or no key was returned, redirect to home
                return RedirectToAction("Index", "Home");
            }
            
            return RedirectToAction("Details", "Trips", new { id = trip.Key });
        }
        catch (Exception)
        {
            // If there's an error creating the trip, redirect back to plan trip
            return RedirectToAction(nameof(PlanTrip), new { userKey = suggestion.UserKey });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RejectSuggestion(string key)
    {
        var suggestion = await _tripSuggestionService.GetByKeyAsync(key);
        if (suggestion == null)
            return RedirectToAction("Index", "Home");

        suggestion.Status = "Rejected";
        await _tripSuggestionService.UpdateAsync(suggestion.Key, suggestion);
        
        return RedirectToAction(nameof(PlanTrip), new { userKey = suggestion.UserKey });
    }
} 