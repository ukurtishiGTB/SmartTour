using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;

namespace SmartTour.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserService _userService;
        private readonly AuthService _authService;

        public ProfileController(UserService userService, AuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        public async Task<IActionResult> Profile()
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userService.GetByKeyAsync(currentUserId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePreferences(string[] preferences, string travelStyle, string budgetRange)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userService.GetByKeyAsync(currentUserId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.Preferences = new System.Collections.Generic.List<string>(preferences ?? new string[0]);
            user.TravelStyle = travelStyle ?? "Adventure";
            user.BudgetRange = budgetRange ?? "Moderate";

            await _userService.UpdateAsync(currentUserId, user);

            TempData["SuccessMessage"] = "Your preferences have been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string name, string location)
        {
            var currentUserId = _authService.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _userService.GetByKeyAsync(currentUserId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            user.Name = name;
            user.Location = location;

            await _userService.UpdateAsync(currentUserId, user);

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }
} 