using Microsoft.AspNetCore.Mvc;
using RaceDayNova.Mvc.Models;
using RaceDayNova.Mvc.Services;
namespace RaceDayNova.Mvc.Controllers;
public class AuthController(ApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.Post<LoginResponseModel>("api/auth/login", new { model.Email, model.Password });
        if (result.Data?.User == null) { ModelState.AddModelError(string.Empty, result.Error ?? "Login failed. Check your details and try again."); return View(model); }
        HttpContext.Session.SetInt32("UserId", result.Data.User.UserId); HttpContext.Session.SetString("UserRole", result.Data.User.UserRole); HttpContext.Session.SetString("UserName", result.Data.User.FirstName + " " + result.Data.User.LastName); HttpContext.Session.SetString("UserEmail", result.Data.User.Email);
        return RedirectToAction("Index", result.Data.User.UserRole == "Organiser" ? "Organiser" : "Participant");
    }
    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await api.Post<UserDto>("api/auth/register", new { model.FirstName, model.LastName, model.Email, model.Password, model.Mobile, model.Role });
        if (result.Data == null) { ModelState.AddModelError(string.Empty, result.Error ?? "Registration failed."); return View(model); }
        TempData["Success"] = "Your account has been created. You can now sign in."; return RedirectToAction(nameof(Login));
    }
    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Logout()
    {
        await api.Post<object>("api/auth/logout", new { }); HttpContext.Session.Clear(); return RedirectToAction("Index", "Home");
    }
}
public class LoginResponseModel { public string Message { get; set; } = ""; public UserDto User { get; set; } = new(); }
