using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Mvc.Models;
using RaceDayNova.Mvc.Services;
namespace RaceDayNova.Mvc.Controllers;
public class ParticipantController(ApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetString("UserRole") == "Participant";
    private IActionResult Guard() => RedirectToAction("Login", "Auth");
    public async Task<IActionResult> Index() { if (!Allowed()) return Guard(); var events = await api.Get<List<EventDto>>("api/events"); var mine = await api.Get<List<EnrolmentDto>>("api/enrolments/me"); ViewBag.Enrolments = mine.Data ?? new(); ViewBag.Error = events.Error ?? mine.Error; return View(events.Data ?? new()); }
    public async Task<IActionResult> MyEvents() { if (!Allowed()) return Guard(); var r = await api.Get<List<EnrolmentDto>>("api/enrolments/me"); ViewBag.Error = r.Error; return View(r.Data ?? new()); }
    public async Task<IActionResult> MyResults() { if (!Allowed()) return Guard(); var r = await api.Get<List<ResultDto>>("api/results/me"); ViewBag.Error = r.Error; return View(r.Data ?? new()); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Enrol(int id, int categoryId) { if (!Allowed()) return Guard(); var r = await api.Post<EnrolmentDto>($"api/events/{id}/enrolments", new { CategoryId = categoryId }); TempData[r.Data != null ? "Success" : "Error"] = r.Data != null ? "You have successfully entered this event." : r.Error ?? "Enrolment failed."; return RedirectToAction("Details", "Events", new { id }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Cancel(int id) { if (!Allowed()) return Guard(); var r = await api.Delete($"api/enrolments/{id}"); TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Enrolment cancelled." : r.Error; return RedirectToAction(nameof(MyEvents)); }
}
