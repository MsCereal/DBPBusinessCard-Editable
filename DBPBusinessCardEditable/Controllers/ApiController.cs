using DBPBusinessCardEditable.Models;
using DBPBusinessCardEditable.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace DBPBusinessCardEditable.Controllers
{
    /// <summary>
    /// DBP Digital Business Card REST API
    /// Base URL: /api/cards
    ///
    /// Authentication: pass ?apiKey=YOUR_KEY on each request
    ///   or set API_KEY environment variable on Railway.
    ///   Default key if not set: dbp-api-2026
    /// </summary>
    [ApiController]
    [Route("api/cards")]
    public class CardsApiController : ControllerBase
    {
        private readonly CardProfileService _service;

        public CardsApiController(CardProfileService service)
        {
            _service = service;
        }

        // ── Auth helper ──────────────────────────────────────
        private bool IsAuthorized()
        {
            string expected = Environment.GetEnvironmentVariable("API_KEY") ?? "dbp-api-2026";
            Request.Headers.TryGetValue("X-Api-Key", out var headerKey);
            Request.Query.TryGetValue("apiKey", out var queryKey);
            return headerKey == expected || queryKey == expected;
        }

        private IActionResult Unauthorized401() =>
            StatusCode(401, new { error = "Unauthorized. Provide a valid API key via X-Api-Key header or ?apiKey= query parameter." });

        // ── GET /api/cards/{empId} ────────────────────────────
        /// <summary>
        /// Get a single employee's digital business card by Employee ID.
        /// Public — no API key required.
        /// Returns 404 if not found.
        /// </summary>
        // GET /api  — API documentation page
        [HttpGet("/api")]
        public IActionResult ApiDocs()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            return Content($@"<!DOCTYPE html>
<html lang='en'><head><meta charset='utf-8'/>
<meta name='viewport' content='width=device-width,initial-scale=1.0'/>
<title>DBP Digital Business Card – API Documentation</title>
<style>
*{{box-sizing:border-box;margin:0;padding:0;}}
body{{font-family:Arial,sans-serif;background:#0f172a;color:#e2e8f0;min-height:100vh;padding:32px 16px 60px;}}
.wrap{{max-width:780px;margin:0 auto;}}
.header{{text-align:center;margin-bottom:40px;}}
.header img{{height:52px;object-fit:contain;margin-bottom:14px;}}
h1{{color:#f1f5f9;font-size:22px;font-weight:800;margin-bottom:6px;}}
.sub{{color:#64748b;font-size:13px;}}
.badge{{display:inline-block;background:rgba(37,99,235,0.2);border:1px solid rgba(37,99,235,0.4);
color:#60a5fa;font-size:10px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;
padding:3px 10px;border-radius:20px;margin-top:10px;}}

.section{{margin-bottom:32px;}}
.section-title{{color:#94a3b8;font-size:11px;font-weight:700;letter-spacing:2px;
text-transform:uppercase;margin-bottom:12px;padding-bottom:8px;
border-bottom:1px solid #1e293b;}}

.endpoint{{background:#1e293b;border:1px solid #334155;border-radius:12px;
padding:20px;margin-bottom:12px;}}
.method-row{{display:flex;align-items:center;gap:10px;margin-bottom:10px;flex-wrap:wrap;}}
.method{{padding:3px 10px;border-radius:6px;font-size:12px;font-weight:800;letter-spacing:0.5px;}}
.get{{background:rgba(22,163,74,0.2);color:#4ade80;border:1px solid rgba(22,163,74,0.3);}}
.post{{background:rgba(37,99,235,0.2);color:#60a5fa;border:1px solid rgba(37,99,235,0.3);}}
.delete{{background:rgba(220,38,38,0.2);color:#f87171;border:1px solid rgba(220,38,38,0.3);}}
.url{{color:#e2e8f0;font-family:monospace;font-size:14px;font-weight:600;}}
.desc{{color:#94a3b8;font-size:13px;margin-bottom:10px;}}
.auth-note{{display:inline-flex;align-items:center;gap:5px;
background:rgba(251,191,36,0.1);border:1px solid rgba(251,191,36,0.2);
color:#fbbf24;font-size:11px;padding:3px 8px;border-radius:6px;}}
.public-note{{display:inline-flex;align-items:center;gap:5px;
background:rgba(22,163,74,0.1);border:1px solid rgba(22,163,74,0.2);
color:#4ade80;font-size:11px;padding:3px 8px;border-radius:6px;}}
.example{{background:#0f172a;border:1px solid #1e293b;border-radius:8px;
padding:12px 14px;margin-top:10px;}}
.example-label{{color:#475569;font-size:10px;font-weight:700;letter-spacing:1px;
text-transform:uppercase;margin-bottom:6px;}}
.example code{{font-family:monospace;font-size:12px;color:#93c5fd;word-break:break-all;}}
pre{{font-family:monospace;font-size:12px;color:#86efac;white-space:pre-wrap;word-break:break-all;}}

.key-box{{background:#1e293b;border:1px solid #334155;border-radius:12px;padding:18px 20px;}}
.key-row{{display:flex;align-items:center;gap:10px;flex-wrap:wrap;}}
.key-label{{color:#64748b;font-size:13px;}}
.key-value{{font-family:monospace;font-size:14px;color:#fbbf24;font-weight:700;}}
.key-hint{{color:#475569;font-size:12px;margin-top:6px;}}

.try-btn{{display:inline-block;margin-top:10px;padding:7px 14px;
background:#2563eb;color:#fff;border-radius:8px;font-size:12px;font-weight:700;
text-decoration:none;transition:background 0.15s;}}
.try-btn:hover{{background:#1d4ed8;}}
</style></head>
<body><div class='wrap'>

<div class='header'>
<img src='/dbp-logo.png' alt='DBP' />
<h1>DBP Digital Business Card</h1>
<p class='sub'>REST API Documentation</p>
<span class='badge'>⚡ API v1.0</span>
</div>

<!-- Base URL -->
<div class='section'>
<p class='section-title'>Base URL</p>
<div class='endpoint'>
<p class='desc'>All API requests go to:</p>
<div class='example'>
<div class='example-label'>Base URL</div>
<code>{baseUrl}/api/cards</code>
</div>
</div>
</div>

<!-- Auth -->
<div class='section'>
<p class='section-title'>Authentication</p>
<div class='key-box'>
<p class='desc' style='margin-bottom:12px;'>Protected endpoints require an API key. Pass it as a header or query parameter:</p>
<div class='key-row'>
<span class='key-label'>Header:</span>
<span class='key-value'>X-Api-Key: dbp-api-2026</span>
</div>
<div class='key-row' style='margin-top:6px;'>
<span class='key-label'>Query:</span>
<span class='key-value'>?apiKey=dbp-api-2026</span>
</div>
<p class='key-hint'>⚠️ Change the default key by setting the API_KEY environment variable on Railway.</p>
</div>
</div>

<!-- Endpoints -->
<div class='section'>
<p class='section-title'>Endpoints</p>

<!-- GET one card -->
<div class='endpoint'>
<div class='method-row'>
<span class='method get'>GET</span>
<span class='url'>/api/cards/{{empId}}</span>
<span class='public-note'>🌐 Public — No key needed</span>
</div>
<p class='desc'>Get a single employee's digital business card by their Employee ID.</p>
<div class='example'>
<div class='example-label'>Example Request</div>
<code>GET {baseUrl}/api/cards/0000001-DBP</code>
</div>
<div class='example' style='margin-top:8px;'>
<div class='example-label'>Example Response</div>
<pre>{{
  ""empId"":       ""0000001-DBP"",
  ""name"":        ""Chelsea De Purificacion"",
  ""title"":       ""Software Engineer"",
  ""org"":         ""Development Bank of the Philippines"",
  ""phone"":       ""+639614475634"",
  ""email"":       ""cdepurificacion@dbp.ph"",
  ""office"":      ""DBP Head Office, Makati City"",
  ""cardUrl"":     ""{baseUrl}/card/0000001-DBP"",
  ""qrUrl"":       ""{baseUrl}/QR/Generate/0000001-DBP"",
  ""lastUpdated"": ""2026-06-23T10:00:00Z""
}}</pre>
</div>
<a href='{baseUrl}/api/cards/0000001-DBP' target='_blank' class='try-btn'>Try it →</a>
</div>

<!-- GET all cards -->
<div class='endpoint'>
<div class='method-row'>
<span class='method get'>GET</span>
<span class='url'>/api/cards</span>
<span class='auth-note'>🔑 Requires API Key</span>
</div>
<p class='desc'>Get all employee cards. Returns card summaries (no photo data).</p>
<div class='example'>
<div class='example-label'>Example Request</div>
<code>GET {baseUrl}/api/cards?apiKey=dbp-api-2026</code>
</div>
<a href='{baseUrl}/api/cards?apiKey=dbp-api-2026' target='_blank' class='try-btn'>Try it →</a>
</div>

<!-- POST card -->
<div class='endpoint'>
<div class='method-row'>
<span class='method post'>POST</span>
<span class='url'>/api/cards</span>
<span class='auth-note'>🔑 Requires API Key</span>
</div>
<p class='desc'>Create or update an employee card. Send JSON in the request body.</p>
<div class='example'>
<div class='example-label'>Example Request Body</div>
<pre>{{
  ""empId"":    ""0000001-DBP"",
  ""name"":     ""Chelsea De Purificacion"",
  ""title"":    ""Software Engineer"",
  ""org"":      ""Development Bank of the Philippines"",
  ""phone"":    ""+639614475634"",
  ""email"":    ""cdepurificacion@dbp.ph"",
  ""office"":   ""Head Office, Makati City""
}}</pre>
</div>
</div>

<!-- DELETE card -->
<div class='endpoint'>
<div class='method-row'>
<span class='method delete'>DELETE</span>
<span class='url'>/api/cards/{{empId}}</span>
<span class='auth-note'>🔑 Requires API Key</span>
</div>
<p class='desc'>Delete a single employee card by Employee ID.</p>
<div class='example'>
<div class='example-label'>Example Request</div>
<code>DELETE {baseUrl}/api/cards/0000001-DBP?apiKey=dbp-api-2026</code>
</div>
</div>

</div><!-- /section -->

<div style='text-align:center;margin-top:32px;'>
<a href='/' style='color:#475569;font-size:12px;text-decoration:none;'>← Back to DBP Digital Business Card</a>
</div>

</div></body></html>", "text/html");
        }
        {
            var card = _service.Get(empId);
            if (card == null)
                return NotFound(new { error = $"No card found for Employee ID: {empId}" });

            return Ok(new
            {
                empId      = card.EmpId,
                name       = card.Name,
                title      = card.Title,
                org        = card.Org,
                phone      = card.Phone,
                email      = card.Email,
                office     = card.Office,
                github     = card.GitHub,
                linkedin   = card.LinkedIn,
                portfolio  = card.Portfolio,
                hasPhoto   = !string.IsNullOrEmpty(card.Photo),
                cardUrl    = $"{Request.Scheme}://{Request.Host}/card/{card.EmpId}",
                qrUrl      = $"{Request.Scheme}://{Request.Host}/QR/Generate/{card.EmpId}",
                lastUpdated = card.LastUpdated
            });
        }

        // ── GET /api/cards ────────────────────────────────────
        /// <summary>
        /// Get all cards. Requires API key.
        /// Returns array of card summaries (no photo data).
        /// </summary>
        [HttpGet]
        public IActionResult GetAllCards()
        {
            if (!IsAuthorized()) return Unauthorized401();

            var cards = _service.GetAll();
            var result = new System.Collections.Generic.List<object>();
            foreach (var card in cards)
            {
                result.Add(new
                {
                    empId     = card.EmpId,
                    name      = card.Name,
                    title     = card.Title,
                    org       = card.Org,
                    phone     = card.Phone,
                    email     = card.Email,
                    office    = card.Office,
                    cardUrl   = $"{Request.Scheme}://{Request.Host}/card/{card.EmpId}",
                    lastUpdated = card.LastUpdated
                });
            }
            return Ok(new { total = result.Count, cards = result });
        }

        // ── POST /api/cards ───────────────────────────────────
        /// <summary>
        /// Create or update a card. Requires API key.
        /// Body: JSON with empId (required), name (required), and optional fields.
        /// </summary>
        [HttpPost]
        public IActionResult UpsertCard([FromBody] CardProfile model)
        {
            if (!IsAuthorized()) return Unauthorized401();

            if (model == null || string.IsNullOrWhiteSpace(model.EmpId))
                return BadRequest(new { error = "empId is required." });

            if (!System.Text.RegularExpressions.Regex.IsMatch(model.EmpId.Trim(), @"^\d{7}-[A-Za-z]{3}$"))
                return BadRequest(new { error = "empId must be in format: 7 digits – 3 letters (e.g. 0000001-DBP)" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { error = "name is required." });

            _service.Save(model);

            return Ok(new
            {
                success = true,
                message = $"Card for {model.EmpId} saved successfully.",
                cardUrl = $"{Request.Scheme}://{Request.Host}/card/{model.EmpId.Trim()}"
            });
        }

        // ── DELETE /api/cards/{empId} ─────────────────────────
        /// <summary>
        /// Delete a single card by Employee ID. Requires API key.
        /// </summary>
        [HttpDelete("{empId}")]
        public IActionResult DeleteCard(string empId)
        {
            if (!IsAuthorized()) return Unauthorized401();

            var card = _service.Get(empId);
            if (card == null)
                return NotFound(new { error = $"No card found for Employee ID: {empId}" });

            _service.Delete(empId);
            return Ok(new { success = true, message = $"Card for {empId} deleted." });
        }
    }
}
