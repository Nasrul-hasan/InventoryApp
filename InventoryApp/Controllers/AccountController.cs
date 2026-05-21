using InventoryApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InventoryApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ─── REGISTER ───────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ─── LOGIN ──────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Blocked user check
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && user.IsBlocked)
            {
                ModelState.AddModelError("", "Your account has been blocked.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }
        // ─── External Login (Google/Facebook) ───────────────
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account",
                new { returnUrl });
            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction("Login");

            // Already has account?
            var result = await _signInManager
                .ExternalLoginSignInAsync(info.LoginProvider,
                                          info.ProviderKey,
                                          isPersistent: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            var email = info.Principal.FindFirstValue(
                System.Security.Claims.ClaimTypes.Email) ??
                info.Principal.FindFirstValue("email") ?? "";
            var name = info.Principal.FindFirstValue(
                System.Security.Claims.ClaimTypes.Name) ?? "FacebookUser";

            // Email fake email ( ifFacebook doesn't provide email ) দিয়ে user তৈরি করো 
            if (string.IsNullOrEmpty(email))
            {
                email = $"{info.ProviderKey}@facebook.com";
            }
            // Email দিয়ে existing user খোঁজো
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Existing account এ Facebook link করো
                await _userManager.AddLoginAsync(existingUser, info);
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(user, info);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Error হলে Login page এ যাও
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            TempData["Error"] = $"User তৈরি হয়নি: {errors}";
            return RedirectToAction("Login");
        }

        // ─── LOGOUT ─────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}