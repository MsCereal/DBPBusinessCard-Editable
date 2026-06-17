using DBPBusinessCardEditable.Models;
using DBPBusinessCardEditable.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;

namespace DBPBusinessCardEditable.Controllers
{
    public class HomeController : Controller
    {
        private readonly CardProfileService _profileService;

        public HomeController(CardProfileService profileService)
        {
            _profileService = profileService;
        }

        // GET /  — Welcome landing page
        public IActionResult Index()
        {
            return View("Welcome");
        }

        // GET /start  — first-time setup form
        [HttpGet("/start")]
        public IActionResult Start()
        {
            return View("Edit", new CardProfile());
        }

        // GET /card/{empId}  — PUBLIC card (what QR scans to) — no edit button
        [HttpGet("/card/{empId}")]
        public IActionResult ViewCard(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null)
                return View("NotFound");
            return View("Card", profile);
        }

        // POST /save  — save the card, redirect to QR entrance
        [HttpPost("/save")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(CardProfile model)
        {
            if (string.IsNullOrWhiteSpace(model.EmpId))
            {
                TempData["Error"] = "Employee ID is required.";
                return View("Edit", model);
            }
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["Error"] = "Full Name is required.";
                return View("Edit", model);
            }
            _profileService.Save(model);
            // After saving, show the QR entrance for this employee
            return RedirectToAction("QRScreen", new { empId = model.EmpId.Trim() });
        }

        // GET /setup  — edit form (new or existing)
        [HttpGet("/setup")]
        public IActionResult Setup(string empId)
        {
            if (!string.IsNullOrWhiteSpace(empId))
            {
                var existing = _profileService.GetOrCreate(empId);
                return View("Edit", existing);
            }
            return View("Edit", new CardProfile());
        }

        // GET /check/{empId}  — validate if an empId card exists (used by Welcome lookup)
        [HttpGet("/check/{empId}")]
        public IActionResult Check(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null)
                return Json(new { exists = false });
            return Json(new { exists = true });
        }

        // GET /qr/{empId}  — employee's QR entrance screen
        [HttpGet("/qr/{empId}")]
        public IActionResult QRScreen(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null)
                return RedirectToAction("Start");
            return View("QREntrance", profile);
        }
        [HttpPost("/reset/{empId}")]
        [ValidateAntiForgeryToken]
        public IActionResult Reset(string empId)
        {
            _profileService.Reset(empId);
            TempData["Reset"] = true;
            return RedirectToAction("Setup", new { empId });
        }

        // GET /download-contact/{empId}  — VCF download for Save to Contacts
        [HttpGet("/download-contact/{empId}")]
        public IActionResult DownloadContact(string empId)
        {
            var p = _profileService.Get(empId);
            if (p == null) return NotFound();

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
            string filename = string.IsNullOrWhiteSpace(p.Name)
                ? $"{empId}-DBP.vcf"
                : $"{p.Name.Replace(" ", "-")}-DBP.vcf";
            return File(bytes, "text/vcard", filename);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
