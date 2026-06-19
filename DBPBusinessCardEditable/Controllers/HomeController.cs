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
            // Validate format: 7 digits - 3 letters (e.g. 0000001-DBP)
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.EmpId.Trim(), @"^\d{7}-[A-Za-z]{3}$"))
            {
                TempData["Error"] = "Employee ID must be in the format: 7 digits – 3 letters (e.g. 0000001-DBP).";
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

        // GET /admin/clear  — admin only, password protected
        [HttpGet("/admin/clear")]
        public IActionResult AdminClear(string key)
        {
            string adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ?? "dbpadmin2026";
            if (key != adminKey)
            {
                return Content(AdminLoginPage(""), "text/html");
            }
            int count = _profileService.Count();
            return Content(AdminDashboard(count, adminKey, null), "text/html");
        }

        // POST /admin/clear  — execute the clear
        [HttpPost("/admin/clear")]
        public IActionResult AdminClearPost(string key, string action)
        {
            string adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ?? "dbpadmin2026";
            if (key != adminKey)
                return Content(AdminLoginPage("Invalid password."), "text/html");

            if (action == "clear")
            {
                int deleted = _profileService.ClearAll();
                return Content(AdminDashboard(0, adminKey, $"{deleted} card(s) deleted successfully."), "text/html");
            }
            return RedirectToAction("AdminClear", new { key });
        }

        // POST /admin/login
        [HttpPost("/admin/login")]
        public IActionResult AdminLogin(string password)
        {
            string adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ?? "dbpadmin2026";
            if (password == adminKey)
                return Redirect($"/admin/clear?key={adminKey}");
            return Content(AdminLoginPage("⚠️ Incorrect password. Access denied."), "text/html");
        }

        private string AdminLoginPage(string error) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'/>
<title>Admin – DBP Digital Business Card</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;
display:flex;align-items:center;justify-content:center;min-height:100vh;padding:20px;}}
.box{{background:#1e293b;border:1px solid #334155;border-radius:16px;
padding:36px 28px;max-width:360px;width:100%;text-align:center;
box-shadow:0 16px 40px rgba(0,0,0,0.4);}}
.lock{{font-size:40px;margin-bottom:16px;}}
h2{{color:#f1f5f9;font-size:20px;font-weight:800;margin-bottom:6px;}}
.sub{{color:#94a3b8;font-size:13px;margin-bottom:8px;}}
.restricted{{display:inline-block;background:rgba(220,38,38,0.15);
border:1px solid rgba(220,38,38,0.3);color:#fca5a5;
font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;
padding:3px 10px;border-radius:20px;margin-bottom:24px;}}
input{{width:100%;padding:12px 14px;border:1.5px solid #334155;border-radius:10px;
background:#0f172a;color:#f1f5f9;font-size:15px;outline:none;margin-bottom:12px;
text-align:center;letter-spacing:2px;}}
input:focus{{border-color:#2563eb;}}
input::placeholder{{letter-spacing:0;color:#475569;font-size:13px;}}
.btn{{width:100%;padding:13px;border:none;border-radius:10px;
background:#2563eb;color:#fff;font-size:15px;font-weight:700;cursor:pointer;}}
.btn:hover{{background:#1d4ed8;}}
.error{{color:#fca5a5;font-size:13px;margin-bottom:14px;
background:rgba(220,38,38,0.1);padding:8px 12px;border-radius:8px;}}
</style></head>
<body><div class='box'>
<div class='lock'>🔐</div>
<h2>Admin Access</h2>
<p class='sub'>DBP Digital Business Card</p>
<span class='restricted'>⚑ Restricted Area</span>
{(string.IsNullOrEmpty(error) ? "" : $"<p class='error'>{error}</p>")}
<form method='post' action='/admin/login'>
<input type='password' name='password' placeholder='Enter admin password' autofocus />
<button type='submit' class='btn'>Unlock Dashboard</button>
</form>
</div></body></html>";

        private string AdminDashboard(int count, string key, string message) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'/>
<title>Admin Dashboard – DBP</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;
display:flex;align-items:center;justify-content:center;min-height:100vh;padding:20px;}}
.box{{background:#1e293b;border:1px solid #334155;border-radius:16px;
padding:36px 28px;max-width:400px;width:100%;text-align:center;
box-shadow:0 16px 40px rgba(0,0,0,0.4);}}
.restricted{{display:inline-block;background:rgba(220,38,38,0.15);
border:1px solid rgba(220,38,38,0.3);color:#fca5a5;
font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;
padding:3px 10px;border-radius:20px;margin-bottom:20px;}}
h2{{color:#f1f5f9;font-size:20px;font-weight:800;margin-bottom:4px;}}
.sub{{color:#64748b;font-size:12px;margin-bottom:24px;}}
.stat-box{{background:#0f172a;border:1px solid #334155;border-radius:12px;
padding:20px;margin-bottom:24px;}}
.stat-num{{font-size:48px;font-weight:800;color:#60a5fa;line-height:1;}}
.stat-label{{color:#64748b;font-size:13px;margin-top:4px;}}
.success{{background:rgba(22,163,74,0.15);border:1px solid rgba(22,163,74,0.3);
color:#4ade80;font-size:13px;padding:10px 14px;border-radius:8px;margin-bottom:20px;}}
.btn{{display:block;width:100%;padding:13px 16px;border-radius:10px;
border:none;font-size:14px;font-weight:700;cursor:pointer;
text-decoration:none;text-align:center;margin-bottom:10px;}}
.btn:last-child{{margin-bottom:0;}}
.btn-danger{{background:#dc2626;color:#fff;}}
.btn-danger:hover{{background:#b91c1c;}}
.btn-home{{background:#334155;color:#f1f5f9;}}
.btn-home:hover{{background:#475569;}}
</style></head>
<body><div class='box'>
<span class='restricted'>⚑ Restricted Area — Admin Only</span>
<h2>Admin Dashboard</h2>
<p class='sub'>DBP Digital Business Card System</p>
{(string.IsNullOrEmpty(message) ? "" : $"<div class='success'>✅ {message}</div>")}
<div class='stat-box'>
<div class='stat-num'>{count}</div>
<div class='stat-label'>Cards currently in database</div>
</div>
<form method='post' action='/admin/clear?key={key}' style='margin-bottom:10px;'>
<input type='hidden' name='key' value='{key}' />
<input type='hidden' name='action' value='clear' />
<button type='submit' class='btn btn-danger'
onclick=""return confirm('Delete all {count} cards? This cannot be undone.')"">
🗑 Delete All Demo Cards
</button>
</form>
<a href='/' class='btn btn-home'>← Back to Home</a>
</div></body></html>";
    }
}
