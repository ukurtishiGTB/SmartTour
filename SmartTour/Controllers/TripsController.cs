// File: Controllers/TripsController.cs

using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using System.Threading.Tasks;

namespace SmartTour.Controllers
{
    public class TripsController : Controller
    {
        private readonly TripService _svc;

        public TripsController(TripService svc)
        {
            _svc = svc;
        }

        // GET: /Trips
        public async Task<IActionResult> Index()
        {
            var allTrips = await _svc.GetAllAsync();
            return View(allTrips); // model: List<Trip>
        }

        // GET: /Trips/Details/{key}
        public async Task<IActionResult> Details(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var trip = await _svc.GetByKeyAsync(key);
            if (trip == null) return NotFound();
            return View(trip); // model: Trip
        }

        // GET: /Trips/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Trips/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Key,UserId,Places,StartDate,EndDate,Notes")] Trip trip)
        {
            if (!ModelState.IsValid) return View(trip);
            await _svc.CreateAsync(trip);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Trips/Edit/{key}
        public async Task<IActionResult> Edit(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var trip = await _svc.GetByKeyAsync(key);
            if (trip == null) return NotFound();
            return View(trip);
        }

        // POST: /Trips/Edit/{key}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string key,
            [Bind("Key,UserId,PlacesCsv,StartDate,EndDate,Notes")] Trip trip)
        {
            if (key != trip.Key) return BadRequest();
            if (!ModelState.IsValid) return View(trip);

            bool success = await _svc.UpdateAsync(key, trip);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to update trip.");
                return View(trip);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Trips/Delete/{key}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            await _svc.DeleteAsync(key);
            return RedirectToAction(nameof(Index));
        }
    }
}