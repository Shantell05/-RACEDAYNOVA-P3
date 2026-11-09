using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Mvc.Models;
using RaceDayNova.Mvc.Services;
namespace RaceDayNova.Mvc.Controllers;
public class HomeController(ApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? search, string? eventType, string? city)
    {
        var query = new List<string>(); if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search)); if (!string.IsNullOrWhiteSpace(eventType)) query.Add("eventType=" + Uri.EscapeDataString(eventType)); if (!string.IsNullOrWhiteSpace(city)) query.Add("city=" + Uri.EscapeDataString(city));
        var result = await api.Get<List<EventDto>>("api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : ""));
        ViewBag.Error = result.Error; ViewBag.Search = search; ViewBag.EventType = eventType; ViewBag.City = city;
        return View(result.Data ?? new List<EventDto>());
    }
    public IActionResult Error() => View();
}
