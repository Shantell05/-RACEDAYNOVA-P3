using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Mvc.Models;
using RaceDayNova.Mvc.Services;
namespace RaceDayNova.Mvc.Controllers;
public class ProfileController(ApiClient api) : Controller
{
    private bool Allowed() => HttpContext.Session.GetInt32("UserId").HasValue;
    public async Task<IActionResult> Index() { if (!Allowed()) return RedirectToAction("Login", "Auth"); var r = await api.Get<UserDto>("api/users/me"); if (r.Data == null) { TempData["Error"] = r.Error; return RedirectToAction("Index", "Home"); } ViewBag.ProfileImageUrl = r.Data.ProfileImageUrl; return View(new ProfileFormModel { FirstName=r.Data.FirstName, LastName=r.Data.LastName, Email=r.Data.Email, Mobile=r.Data.Mobile }); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Index(ProfileFormModel model) { if (!Allowed()) return RedirectToAction("Login", "Auth"); if (!ModelState.IsValid) return View(model); var r = await api.Put<UserDto>("api/users/me", new { model.FirstName, model.LastName, model.Email, model.Mobile, model.CurrentPassword, model.NewPassword }); if (r.Data == null) { ModelState.AddModelError("", r.Error ?? "Profile could not be updated."); return View(model); } HttpContext.Session.SetString("UserName", r.Data.FirstName + " " + r.Data.LastName); HttpContext.Session.SetString("UserEmail", r.Data.Email); if (model.ProfilePicture != null) { var upload = await api.Upload("api/users/me/profile-picture", model.ProfilePicture); if (upload.Error != null) TempData["Error"] = "Profile saved, but picture upload failed: " + upload.Error; else TempData["Success"] = "Profile and picture updated successfully."; } else TempData["Success"] = "Profile updated successfully."; return RedirectToAction(nameof(Index)); }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> SwitchRole(string role)
    {
        if (!Allowed()) return RedirectToAction("Login", "Auth");
        var r = await api.Put<UserDto>("api/users/me/role", new { Role = role });
        if (r.Data == null) { TempData["Error"] = r.Error ?? "Could not switch role. Please try again."; return RedirectToAction(nameof(Index)); }
        HttpContext.Session.SetString("UserRole", r.Data.UserRole);
        TempData["Success"] = $"You are now using RaceDay Nova as an {r.Data.UserRole}.";
        return RedirectToAction("Index", r.Data.UserRole == "Organiser" ? "Organiser" : "Participant");
    }
}
