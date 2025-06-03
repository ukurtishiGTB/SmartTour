// File: Controllers/RecommendationsController.cs

using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using System.Threading.Tasks;

namespace SmartTour.Controllers
{
    public class RecommendationsController : Controller
    {
        private readonly RecommendationService _svc;

        public RecommendationsController(RecommendationService svc)
        {
            _svc = svc;
        }

        // GET: /Recommendations/ByPlace/{placeKey}
        public async Task<IActionResult> ByPlace(string placeKey)
        {
            if (string.IsNullOrEmpty(placeKey)) return BadRequest();

            var results = await _svc.RecommendByPlaceAsync(placeKey);
            ViewBag.PlaceKey = placeKey;
            return View(results); // model: List<RecommendationResult>
        }

        // GET: /Recommendations/ByTags/{userKey}
        public async Task<IActionResult> ByTags(string userKey)
        {
            if (string.IsNullOrEmpty(userKey)) return BadRequest();

            var results = await _svc.RecommendByTagsAsync(userKey);
            ViewBag.UserKey = userKey;
            return View(results); // model: List<Place>
        }
    }
}