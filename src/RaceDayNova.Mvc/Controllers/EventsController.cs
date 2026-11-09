using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Mvc.Models;
using RaceDayNova.Mvc.Services;
namespace RaceDayNova.Mvc.Controllers;
public class EventsController(ApiClient api) : Controller
{
    public async Task<IActionResult> Details(int id) { var r = await api.Get<EventDto>($"api/events/{id}"); if (r.Data == null) { TempData["Error"] = r.Error ?? "Event not found."; return RedirectToAction("Index", "Home"); } ViewBag.Categories = r.Data.Categories; var weather = await api.Get<WeatherViewModel>($"api/events/{id}/weather"); ViewBag.Weather = weather.Data; return View(r.Data); }
}
