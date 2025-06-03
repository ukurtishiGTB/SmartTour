// File: Controllers/UsersController.cs

using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Services;
using System.Threading.Tasks;

namespace SmartTour.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserService _svc;

        public UsersController(UserService svc)
        {
            _svc = svc;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            var allUsers = await _svc.GetAllAsync();
            return View(allUsers); // model: List<User>
        }

        // GET: /Users/Details/{key}
        public async Task<IActionResult> Details(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var user = await _svc.GetByKeyAsync(key);
            if (user == null) return NotFound();
            return View(user); // model: User
        }

        // GET: /Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Key,Name,Email,Preferences")] User user)
        {
            if (!ModelState.IsValid) return View(user);
            await _svc.CreateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/{key}
        public async Task<IActionResult> Edit(string key)
        {
            if (string.IsNullOrEmpty(key)) return BadRequest();
            var user = await _svc.GetByKeyAsync(key);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Users/Edit/{key}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string key, [Bind("Key,Name,Email,Preferences")] User user)
        {
            if (key != user.Key) return BadRequest();
            if (!ModelState.IsValid) return View(user);

            bool updated = await _svc.UpdateAsync(key, user);
            if (!updated)
            {
                ModelState.AddModelError("", "Failed to update user.");
                return View(user);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Users/Delete/{key}
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