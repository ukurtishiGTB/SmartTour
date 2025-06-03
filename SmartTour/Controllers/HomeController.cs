using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartTour.Models;
using SmartTour.Data;

namespace SmartTour.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ArangoDBHelper _arangoDBHelper;

    public HomeController(ILogger<HomeController> logger, ArangoDBHelper arangoDBHelper)
    {
        _logger = logger;
        _arangoDBHelper= arangoDBHelper;
    }

    public async Task<IActionResult> Index()
    {
        //bool isAlive = await _arangoDBHelper.IsServerAliveAsync();

        // Pass a simple string into ViewBag so the view can render it
        /*ViewBag.ArangoStatus = isAlive
            ? "✅ ArangoDB is reachable."
            : "❌ Cannot reach ArangoDB.";
        */
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}