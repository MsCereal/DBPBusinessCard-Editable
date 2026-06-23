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

        // GET /
        public IActionResult Index() => View("Welcome");

        // GET /start
        [HttpGet("/start")]
        public IActionResult Start() => View("Edit", new CardProfile());

        // GET /card/{empId}
        [HttpGet("/card/{empId}")]
        public IActionResult ViewCard(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null) return View("NotFound");
            return View("Card", profile);
        }

        // POST /save
        [HttpPost("/save")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(CardProfile model)
        {
            if (string.IsNullOrWhiteSpace(model.EmpId))
            {
                TempData["Error"] = "Employee ID is required.";
                return View("Edit", model);
            }
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
            return RedirectToAction("QRScreen", new { empId = model.EmpId.Trim() });
        }

        // GET /setup
        [HttpGet("/setup")]
        public IActionResult Setup(string empId)
        {
            if (!string.IsNullOrWhiteSpace(empId))
                return View("Edit", _profileService.GetOrCreate(empId));
            return View("Edit", new CardProfile());
        }

        // GET /check/{empId}
        [HttpGet("/check/{empId}")]
        public IActionResult Check(string empId)
        {
            var profile = _profileService.Get(empId);
            return Json(new { exists = profile != null });
        }

        // GET /qr/{empId}
        [HttpGet("/qr/{empId}")]
        public IActionResult QRScreen(string empId)
        {
            var profile = _profileService.Get(empId);
            if (profile == null) return RedirectToAction("Start");
            return View("QREntrance", profile);
        }

        // POST /reset/{empId}
        [HttpPost("/reset/{empId}")]
        [ValidateAntiForgeryToken]
        public IActionResult Reset(string empId)
        {
            _profileService.Reset(empId);
            TempData["Reset"] = true;
            return RedirectToAction("Setup", new { empId });
        }

        // GET /download-contact/{empId}
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

        // GET /api — API documentation page
        [HttpGet("/api")]
        public IActionResult ApiDocs()
        {
            string b = $"{Request.Scheme}://{Request.Host}";
            return Content($@"<!DOCTYPE html>
<html lang='en'><head><meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1.0'/>
<title>DBP Digital Business Card – API Docs</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;color:#e2e8f0;min-height:100vh;padding:32px 16px 60px;}}
.wrap{{max-width:760px;margin:0 auto;}}
.header{{text-align:center;margin-bottom:36px;}}
.header img{{height:50px;object-fit:contain;margin-bottom:12px;}}
h1{{color:#f1f5f9;font-size:21px;font-weight:800;margin-bottom:5px;}}
.sub{{color:#64748b;font-size:13px;}}
.badge{{display:inline-block;background:rgba(37,99,235,0.2);border:1px solid rgba(37,99,235,0.4);color:#60a5fa;font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;padding:3px 10px;border-radius:20px;margin-top:8px;}}
.stitle{{color:#94a3b8;font-size:11px;font-weight:700;letter-spacing:2px;text-transform:uppercase;margin-bottom:10px;padding-bottom:8px;border-bottom:1px solid #1e293b;}}
.section{{margin-bottom:28px;}}
.ep{{background:#1e293b;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:10px;}}
.mr{{display:flex;align-items:center;gap:10px;margin-bottom:8px;flex-wrap:wrap;}}
.m{{padding:3px 10px;border-radius:6px;font-size:12px;font-weight:800;}}
.get{{background:rgba(22,163,74,0.2);color:#4ade80;border:1px solid rgba(22,163,74,0.3);}}
.post{{background:rgba(37,99,235,0.2);color:#60a5fa;border:1px solid rgba(37,99,235,0.3);}}
.del{{background:rgba(220,38,38,0.2);color:#f87171;border:1px solid rgba(220,38,38,0.3);}}
.url{{color:#e2e8f0;font-family:monospace;font-size:14px;font-weight:600;}}
.desc{{color:#94a3b8;font-size:13px;margin-bottom:8px;}}
.an{{display:inline-flex;align-items:center;gap:5px;background:rgba(251,191,36,0.1);border:1px solid rgba(251,191,36,0.2);color:#fbbf24;font-size:11px;padding:3px 8px;border-radius:6px;}}
.pn{{display:inline-flex;align-items:center;gap:5px;background:rgba(22,163,74,0.1);border:1px solid rgba(22,163,74,0.2);color:#4ade80;font-size:11px;padding:3px 8px;border-radius:6px;}}
.ex{{background:#0f172a;border:1px solid #1e293b;border-radius:8px;padding:10px 12px;margin-top:8px;}}
.el{{color:#475569;font-size:10px;font-weight:700;letter-spacing:1px;text-transform:uppercase;margin-bottom:4px;}}
code{{font-family:monospace;font-size:12px;color:#93c5fd;word-break:break-all;}}
pre{{font-family:monospace;font-size:12px;color:#86efac;white-space:pre-wrap;word-break:break-all;}}
.kb{{background:#1e293b;border:1px solid #334155;border-radius:12px;padding:16px 18px;}}
.kv{{font-family:monospace;font-size:14px;color:#fbbf24;font-weight:700;}}
.kl{{color:#64748b;font-size:13px;}}
.kh{{color:#475569;font-size:12px;margin-top:6px;}}
.tb{{display:inline-block;margin-top:8px;padding:6px 14px;background:#2563eb;color:#fff;border-radius:8px;font-size:12px;font-weight:700;text-decoration:none;}}
.tb:hover{{background:#1d4ed8;}}
</style></head>
<body><div class='wrap'>
<div class='header'>
<img src='/dbp-logo.png' alt='DBP'/>
<h1>DBP Digital Business Card</h1>
<p class='sub'>REST API Documentation</p>
<span class='badge'>API v1.0</span>
</div>
<div class='section'><p class='stitle'>Base URL</p>
<div class='ep'><p class='desc'>All endpoints are relative to:</p>
<div class='ex'><div class='el'>Base URL</div><code>{b}/api/cards</code></div></div></div>
<div class='section'><p class='stitle'>Authentication</p>
<div class='kb'>
<p class='desc' style='margin-bottom:10px;'>Protected endpoints require an API key:</p>
<div style='display:flex;gap:16px;flex-wrap:wrap;'>
<div><span class='kl'>Header: </span><span class='kv'>X-Api-Key: dbp-api-2026</span></div>
<div><span class='kl'>Query: </span><span class='kv'>?apiKey=dbp-api-2026</span></div>
</div>
<p class='kh'>Set API_KEY environment variable on Railway to change the default key.</p>
</div></div>
<div class='section'><p class='stitle'>Endpoints</p>
<div class='ep'>
<div class='mr'><span class='m get'>GET</span><span class='url'>/api/cards/{{empId}}</span><span class='pn'>Public</span></div>
<p class='desc'>Get one employee card by Employee ID. No key required.</p>
<div class='ex'><div class='el'>Example</div><code>GET {b}/api/cards/0205861-CHE</code></div>
<a href='{b}/api/cards/0205861-CHE' target='_blank' class='tb'>Try it</a>
</div>
<div class='ep'>
<div class='mr'><span class='m get'>GET</span><span class='url'>/api/cards</span><span class='an'>API Key Required</span></div>
<p class='desc'>Get all employee cards (no photo data).</p>
<div class='ex'><div class='el'>Example</div><code>GET {b}/api/cards?apiKey=dbp-api-2026</code></div>
<a href='{b}/api/cards?apiKey=dbp-api-2026' target='_blank' class='tb'>Try it</a>
</div>
<div class='ep'>
<div class='mr'><span class='m post'>POST</span><span class='url'>/api/cards</span><span class='an'>API Key Required</span></div>
<p class='desc'>Create or update a card. Send JSON body with empId and name required.</p>
<div class='ex'><div class='el'>Request Body</div>
<pre>{{
  ""empId"":  ""0000001-DBP"",
  ""name"":   ""Juan Dela Cruz"",
  ""title"":  ""Branch Manager"",
  ""org"":    ""Development Bank of the Philippines"",
  ""phone"":  ""+639001234567"",
  ""email"":  ""jdelacruz@dbp.ph"",
  ""office"": ""DBP Head Office, Makati City""
}}</pre></div>
</div>
<div class='ep'>
<div class='mr'><span class='m del'>DELETE</span><span class='url'>/api/cards/{{empId}}</span><span class='an'>API Key Required</span></div>
<p class='desc'>Delete one employee card by Employee ID.</p>
<div class='ex'><div class='el'>Example</div><code>DELETE {b}/api/cards/0000001-DBP?apiKey=dbp-api-2026</code></div>
</div>
</div>
<div style='text-align:center;margin-top:28px;'>
<a href='/' style='color:#475569;font-size:12px;text-decoration:none;'>← Back to Home</a>
</div>
</div></body></html>", "text/html");
        }

        // GET /admin/clear
        [HttpGet("/admin/clear")]
        public IActionResult AdminClear(string key)
        {
            string adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ?? "dbpadmin2026";
            if (key != adminKey) return Content(AdminLoginPage(""), "text/html");
            return Content(AdminDashboard(_profileService.Count(), adminKey, null), "text/html");
        }

        // POST /admin/clear
        [HttpPost("/admin/clear")]
        public IActionResult AdminClearPost(string key, string action)
        {
            string adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY") ?? "dbpadmin2026";
            if (key != adminKey) return Content(AdminLoginPage("Invalid password."), "text/html");
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
            if (password == adminKey) return Redirect($"/admin/clear?key={adminKey}");
            return Content(AdminLoginPage("⚠️ Incorrect password. Access denied."), "text/html");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        private string AdminLoginPage(string error) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'/><title>Admin – DBP</title>
<style>*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;display:flex;align-items:center;justify-content:center;min-height:100vh;padding:20px;}}
.box{{background:#1e293b;border:1px solid #334155;border-radius:16px;padding:36px 28px;max-width:360px;width:100%;text-align:center;}}
.lock{{font-size:40px;margin-bottom:14px;}}
h2{{color:#f1f5f9;font-size:20px;font-weight:800;margin-bottom:5px;}}
.sub{{color:#94a3b8;font-size:13px;margin-bottom:8px;}}
.badge{{display:inline-block;background:rgba(220,38,38,0.15);border:1px solid rgba(220,38,38,0.3);color:#fca5a5;font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;padding:3px 10px;border-radius:20px;margin-bottom:22px;}}
input{{width:100%;padding:12px 14px;border:1.5px solid #334155;border-radius:10px;background:#0f172a;color:#f1f5f9;font-size:15px;outline:none;margin-bottom:12px;text-align:center;letter-spacing:2px;}}
input:focus{{border-color:#2563eb;}}
input::placeholder{{letter-spacing:0;color:#475569;font-size:13px;}}
.btn{{width:100%;padding:13px;border:none;border-radius:10px;background:#2563eb;color:#fff;font-size:15px;font-weight:700;cursor:pointer;}}
.btn:hover{{background:#1d4ed8;}}
.error{{color:#fca5a5;font-size:13px;margin-bottom:14px;background:rgba(220,38,38,0.1);padding:8px 12px;border-radius:8px;}}
</style></head>
<body><div class='box'>
<div class='lock'>🔐</div>
<h2>Admin Access</h2>
<p class='sub'>DBP Digital Business Card</p>
<span class='badge'>Restricted Area</span>
{(string.IsNullOrEmpty(error) ? "" : $"<p class='error'>{error}</p>")}
<form method='post' action='/admin/login'>
<input type='password' name='password' placeholder='Enter admin password' autofocus/>
<button type='submit' class='btn'>Unlock Dashboard</button>
</form>
</div></body></html>";

        private string AdminDashboard(int count, string key, string message) => $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'/><title>Admin Dashboard – DBP</title>
<style>*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;display:flex;align-items:center;justify-content:center;min-height:100vh;padding:20px;}}
.box{{background:#1e293b;border:1px solid #334155;border-radius:16px;padding:36px 28px;max-width:400px;width:100%;text-align:center;}}
.badge{{display:inline-block;background:rgba(220,38,38,0.15);border:1px solid rgba(220,38,38,0.3);color:#fca5a5;font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;padding:3px 10px;border-radius:20px;margin-bottom:18px;}}
h2{{color:#f1f5f9;font-size:20px;font-weight:800;margin-bottom:4px;}}
.sub{{color:#64748b;font-size:12px;margin-bottom:22px;}}
.stat{{background:#0f172a;border:1px solid #334155;border-radius:12px;padding:18px;margin-bottom:22px;}}
.num{{font-size:48px;font-weight:800;color:#60a5fa;line-height:1;}}
.lbl{{color:#64748b;font-size:13px;margin-top:4px;}}
.success{{background:rgba(22,163,74,0.15);border:1px solid rgba(22,163,74,0.3);color:#4ade80;font-size:13px;padding:10px 14px;border-radius:8px;margin-bottom:18px;}}
.btn{{display:block;width:100%;padding:13px;border-radius:10px;border:none;font-size:14px;font-weight:700;cursor:pointer;text-decoration:none;text-align:center;margin-bottom:8px;}}
.btn:last-child{{margin-bottom:0;}}
.btn-d{{background:#dc2626;color:#fff;}}
.btn-d:hover{{background:#b91c1c;}}
.btn-h{{background:#334155;color:#f1f5f9;}}
.btn-h:hover{{background:#475569;}}
</style></head>
<body><div class='box'>
<span class='badge'>Restricted Area — Admin Only</span>
<h2>Admin Dashboard</h2>
<p class='sub'>DBP Digital Business Card System</p>
{(string.IsNullOrEmpty(message) ? "" : $"<div class='success'>✅ {message}</div>")}
<div class='stat'><div class='num'>{count}</div><div class='lbl'>Cards in database</div></div>
<form method='post' action='/admin/clear?key={key}' style='margin-bottom:8px;'>
<input type='hidden' name='key' value='{key}'/>
<input type='hidden' name='action' value='clear'/>
<button type='submit' class='btn btn-d' onclick=""return confirm('Delete all {count} cards? Cannot be undone.')"">🗑 Delete All Demo Cards</button>
</form>
<a href='/' class='btn btn-h'>← Back to Home</a>
</div></body></html>";
    }
}
