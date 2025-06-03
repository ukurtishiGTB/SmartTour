// File: Controllers/PlacesController.cs

using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using System.Threading.Tasks;

namespace SmartTour.Controllers
{
    public class PlacesController : Controller
    {
        private readonly PlaceService _svc;

        public PlacesController(PlaceService svc)
        {
            _svc = svc;
        }

        // GET: /Places
        public async Task<IActionResult> Index()
        {
            var allPlaces = await _svc.GetAllAsync();
            return View(allPlaces); // model: List<Place>
        }

        // GET: /Places/Details/{key}
        public async Task<IActionResult> Details(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var place = await _svc.GetByKeyAsync(key);
            if (place == null) return NotFound();
            return View(place); // model: Place
        }

        // GET: /Places/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Places/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Key,Name,Type,City,Country,Tags,Coordinates")] Place place)
        {
            if (!ModelState.IsValid) return View(place);
            await _svc.CreateAsync(place);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Places/Edit/{key}
        public async Task<IActionResult> Edit(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var place = await _svc.GetByKeyAsync(key);
            if (place == null) return NotFound();
            return View(place);
        }

        // POST: /Places/Edit/{key}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string key, [Bind("Key,Name,Type,City,Country,Tags,Coordinates")] Place place)
        {
            if (key != place.Key) return BadRequest();
            if (!ModelState.IsValid) return View(place);

            bool success = await _svc.UpdateAsync(key, place);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to update place.");
                return View(place);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Places/Delete/{key}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            await _svc.DeleteAsync(key);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Places/SearchByTag?tag=beach
        public async Task<IActionResult> SearchByTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return RedirectToAction(nameof(Index));

            var results = await _svc.SearchByTagAsync(tag);
            ViewBag.SearchTag = tag;
            return View(results); // model: List<Place>
        }
    }
}