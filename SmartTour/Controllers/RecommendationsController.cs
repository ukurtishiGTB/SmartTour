// File: Controllers/RecommendationsController.cs

using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Collections.Generic;

namespace SmartTour.Controllers
{
    public class RecommendationsController : Controller
    {
        private readonly RecommendationService _recommendationService;
        private readonly AuthService _authService;
        private readonly UserService _userService;

        public RecommendationsController(
            RecommendationService recommendationService, 
            AuthService authService,
            UserService userService)
        {
            _recommendationService = recommendationService;
            _authService = authService;
            _userService = userService;
        }

        // GET: /Recommendations/ByPlace/{placeKey}
        public async Task<IActionResult> ByPlace(string placeKey)
        {
            if (string.IsNullOrEmpty(placeKey)) return BadRequest();

            var results = await _recommendationService.RecommendByPlaceAsync(placeKey);
            ViewBag.PlaceKey = placeKey;
            return View(results); // model: List<RecommendationResult>
        }

        // GET: /Recommendations/ByTags/{userKey}
        public async Task<IActionResult> ByTags(string userKey)
        {
            if (string.IsNullOrEmpty(userKey)) return BadRequest();

            var results = await _recommendationService.RecommendByTagsAsync(userKey);
            ViewBag.UserKey = userKey;
            return View(results); // model: List<Place>
        }

        // GET: /Recommendations/PersonalizedTrip
        [Authorize]
        public async Task<IActionResult> PersonalizedTrip(int duration = 7)
        {
            var userKey = _authService.GetCurrentUserId();
            
            // Get user preferences
            var user = await _userService.GetByKeyAsync(userKey);
            ViewBag.HasPreferences = user?.Preferences != null && user.Preferences.Any();
            
            // Set the duration in ViewBag for the view
            ViewBag.Duration = duration;

            // If user has no preferences, return empty result with HasPreferences = false
            if (!ViewBag.HasPreferences)
            {
                return View(Enumerable.Empty<Place>());
            }

            // Generate recommendations
            var results = await _recommendationService.GeneratePersonalizedTripAsync(userKey, duration);
            
            // If no results but user has preferences, they will be generated asynchronously
            if (!results.Any())
            {
                // The view will handle showing a loading state and auto-refreshing
                return View(results);
            }

            return View(results);
        }

        // GET: /Recommendations/NearbyPlaces/{cityName}
        public async Task<IActionResult> NearbyPlaces(string cityName)
        {
            if (string.IsNullOrEmpty(cityName)) return BadRequest();

            var results = await _recommendationService.RecommendNearbyPlacesAsync(cityName);
            ViewBag.CityName = cityName;
            return View(results); // model: List<Place>
        }

        // GET: /Recommendations/ConnectedPlaces/{placeKey}
        public async Task<IActionResult> ConnectedPlaces(string placeKey, int depth = 2)
        {
            if (string.IsNullOrEmpty(placeKey)) return BadRequest();

            var results = await _recommendationService.FindConnectedPlacesAsync(placeKey, depth);
            ViewBag.PlaceKey = placeKey;
            ViewBag.Depth = depth;
            return View(results); // model: List<Place>
        }

        // GET: /Recommendations/SimilarUsers/{userKey}
        [Authorize]
        public async Task<IActionResult> SimilarUsers(int minCommonPlaces = 2)
        {
            var userKey = _authService.GetCurrentUserId();
            var results = await _recommendationService.FindSimilarUsersAsync(userKey, minCommonPlaces);
            return View(results); // model: List<User>
        }
    }
}