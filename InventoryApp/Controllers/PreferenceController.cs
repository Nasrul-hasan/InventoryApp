using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.Controllers
{
    public class PreferenceController : Controller
    {
        // ─── Theme Toggle ─────────────────────────────────
        [HttpPost]
        public IActionResult SetTheme(string theme, string returnUrl = "/")
        {
            Response.Cookies.Append("theme", theme, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return Redirect(returnUrl);
        }

        // ─── Language Toggle ──────────────────────────────
        [HttpPost]
        public IActionResult SetLanguage(string language, string returnUrl = "/")
        {
            Response.Cookies.Append("language", language, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return Redirect(returnUrl);
        }
    }
}