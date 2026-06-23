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
        [HttpGet("{empId}")]
        public IActionResult GetCard(string empId)
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
