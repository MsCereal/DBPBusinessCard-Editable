using DBPBusinessCardEditable.Models;
using DBPBusinessCardEditable.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace DBPBusinessCardEditable.Controllers
{
    public class HomeController : Controller
    {
        private readonly CardProfileService _profileService;
        private const string CookieName = "dbp_user_id";

        public HomeController(CardProfileService profileService)
        {
            _profileService = profileService;
        }

        // GET /  — QR scan landing: show the user's card (view-only)
        public IActionResult Index()
        {
            string userId = GetOrCreateUserId();
            var profile = _profileService.GetOrCreate(userId);
            return View("Card", profile);
        }

        // GET /card/{userId}  — direct link via QR code, view someone else's card
        [HttpGet("/card/{userId}")]
        public IActionResult ViewCard(string userId)
        {
            var profile = _profileService.Get(userId);
            if (profile == null)
                profile = new CardProfile { UserId = userId };
            return View("Card", profile);
        }

        // GET /edit  — show the edit form for the current user
        [HttpGet("/edit")]
        public IActionResult Edit()
        {
            string userId = GetOrCreateUserId();
            var profile = _profileService.GetOrCreate(userId);
            return View(profile);
        }

        // POST /edit  — save the filled-in profile
        [HttpPost("/edit")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CardProfile model)
        {
            string userId = GetOrCreateUserId();
            model.UserId = userId;
            _profileService.Save(model);
            TempData["Saved"] = true;
            return RedirectToAction(nameof(Edit));
        }

        // POST /reset  — clear the profile back to blank
        [HttpPost("/reset")]
        [ValidateAntiForgeryToken]
        public IActionResult Reset()
        {
            string userId = GetOrCreateUserId();
            _profileService.Reset(userId);
            TempData["Reset"] = true;
            return RedirectToAction(nameof(Edit));
        }

        // GET /download-contact  — dynamic VCF from current user's profile
        [HttpGet("/download-contact")]
        public IActionResult DownloadContact()
        {
            string userId = GetOrCreateUserId();
            var p = _profileService.GetOrCreate(userId);

            string vcf =
                "BEGIN:VCARD\r\n" +
                "VERSION:3.0\r\n" +
                $"FN:{p.Name}\r\n" +
                $"ORG:{p.Org}\r\n" +
                $"TITLE:{p.Title}\r\n" +
                $"TEL;TYPE=CELL:{p.Phone}\r\n" +
                $"EMAIL:{p.Email}\r\n" +
                $"URL:{p.GitHub}\r\n" +
                "END:VCARD\r\n";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(vcf);
            string filename = string.IsNullOrWhiteSpace(p.Name) ? "contact.vcf" : $"{p.Name.Replace(" ", "-")}-DBP.vcf";
            return File(bytes, "text/vcard", filename);
        }

        // GET /Home/Index  — kept for backward compat (QR code points here)
        [HttpGet("/Home/Index")]
        public IActionResult HomeIndex()
        {
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        // ── Helpers ─────────────────────────────────────────────
        private string GetOrCreateUserId()
        {
            if (Request.Cookies.TryGetValue(CookieName, out string existing) && !string.IsNullOrWhiteSpace(existing))
                return existing;

            string newId = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(CookieName, newId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(10),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
            return newId;
        }
    }
}
