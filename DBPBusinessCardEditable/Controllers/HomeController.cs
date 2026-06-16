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

        // GET /  — landing: show the edit/setup form
        public IActionResult Index()
        {
            return View("Edit", new CardProfile());
        }

        // GET /card/{empId}  — public card link (what the QR code points to)
        [HttpGet("/card/{empId}")]
        public IActionResult ViewCard(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null)
                return View("NotFound");
            return View("Card", profile);
        }

        // GET /edit/{empId}  — edit an existing card by empId
        [HttpGet("/edit/{empId}")]
        public IActionResult Edit(string empId)
        {
            var profile = _profileService.GetOrCreate(empId);
            return View("Edit", profile);
        }

        // POST /save  — save/create the card
        [HttpPost("/save")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(CardProfile model)
        {
            if (string.IsNullOrWhiteSpace(model.EmpId))
            {
                TempData["Error"] = "Employee ID is required.";
                return View("Edit", model);
            }

            _profileService.Save(model);
            // Redirect to the card page so they see the result + QR
            return RedirectToAction(nameof(ViewCard), new { empId = model.EmpId.Trim() });
        }

        // POST /reset/{empId}  — reset the card back to blank
        [HttpPost("/reset/{empId}")]
        [ValidateAntiForgeryToken]
        public IActionResult Reset(string empId)
        {
            _profileService.Reset(empId);
            return RedirectToAction(nameof(Edit), new { empId });
        }

        // GET /download-contact/{empId}  — VCF download
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
