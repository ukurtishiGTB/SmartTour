// File: Controllers/TripsController.cs

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using SmartTour.ViewModels;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace SmartTour.Controllers
{
    [Authorize]
    public class TripsController : Controller
    {
        private readonly TripService _tripService;
        private readonly AuthService _authService;
        private readonly UserService _userService;
        private readonly RecommendationService _recommendationService;
        private readonly ILogger<TripsController> _logger;

        public TripsController(
            TripService tripService,
            AuthService authService,
            UserService userService,
            RecommendationService recommendationService,
            ILogger<TripsController> logger)
        {
            _tripService = tripService;
            _authService = authService;
            _userService = userService;
            _recommendationService = recommendationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var trips = await _tripService.GetAllAsync();
            return View(trips);
        }
        // GET: Trips/Create
        public IActionResult Create(string placeNames)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new TripCreateViewModel();
            
            if (!string.IsNullOrEmpty(placeNames))
            {
                var placeNamesList = placeNames.Split(',').Select(p => p.Trim()).ToArray();
                model.Places = placeNamesList.Select(name => new PlaceInputViewModel { Name = name }).ToList();
            }
            
            return View(model);
        }

        // POST: Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TripCreateViewModel model)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        _logger.LogError($"Model validation error: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            var trip = new Trip
            {
                Name = model.Name,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                UserKey = currentUserId,
                Places = model.Places?.Select(p => new Place { Name = p.Name }).ToList() ?? new List<Place>(),
                Budget = model.Budget,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "Planned"
            };

            try
            {
                var tripId = await _tripService.CreateAsync(trip);
                _logger.LogInformation($"Created trip with ID: {tripId}");
                TempData["SuccessMessage"] = "Trip created successfully!";
                return RedirectToAction(nameof(MyTrips));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating trip: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while creating the trip. Please try again.");
                return View(model);
            }
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var trip = await _tripService.GetTripAsync(id);
            if (trip == null || trip.UserKey != currentUserId)
            {
                return NotFound();
            }

            var model = new TripEditViewModel
            {
                Key = trip.Id,
                Name = trip.Name,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Budget = trip.Budget,
                Places = trip.Places
                    .Select(p => new PlaceInputViewModel { Name = p.Name })
                    .ToList()
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TripEditViewModel model)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            model.UserKey = currentUserId;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var trip = await _tripService.GetTripAsync(model.Key);
            if (trip == null || trip.UserKey != currentUserId)
            {
                return NotFound();
            }

            trip.Name = model.Name;
            trip.Description = model.Description;
            trip.StartDate = model.StartDate;
            trip.EndDate = model.EndDate;
            trip.Budget = model.Budget;
            trip.UpdatedAt = DateTime.UtcNow;

            // Replace places
            trip.Places = model.Places?
                .Select(p => new Place { Name = p.Name })
                .ToList() 
                ?? new List<Place>();

            try
            {
                await _tripService.UpdateAsync(trip.Key, trip);
                _logger.LogInformation($"Updated trip with ID: {trip.Id}");
                TempData["SuccessMessage"] = "Trip updated successfully!";
                return RedirectToAction(nameof(MyTrips));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating trip: {ex.Message}");
                ModelState.AddModelError("", "An error occurred while updating the trip. Please try again.");
                return View(model);
            }
        }

        public async Task<IActionResult> MyTrips()
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation($"Fetching trips for user: {currentUserId}");

            var upcomingTrips = await _tripService.GetUpcomingTripsAsync(currentUserId);
            var pastTrips = await _tripService.GetPastTripsAsync(currentUserId);

            _logger.LogInformation($"Found {upcomingTrips.Count()} upcoming trips and {pastTrips.Count()} past trips");

            var viewModel = new MyTripsViewModel
            {
                UpcomingTrips = upcomingTrips,
                PastTrips = pastTrips
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string id)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _tripService.GetTripAsync(id);
            if (trip == null || trip.UserKey != currentUserId)
            {
                return NotFound();
            }

            return View(trip);
        }
        

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var trip = await _tripService.GetTripAsync(id);
            if (trip == null || trip.UserKey != currentUserId)
            {
                return NotFound();
            }

            await _tripService.DeleteTripAsync(id);
            TempData["SuccessMessage"] = "Trip deleted successfully.";
            return RedirectToAction(nameof(MyTrips));
        }

        
    }
}