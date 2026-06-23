using DBPBusinessCardEditable.Models;
using DBPBusinessCardEditable.Services;
using Microsoft.AspNetCore.Mvc;
using System;

namespace DBPBusinessCardEditable.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardsApiController : ControllerBase
    {
        private readonly CardProfileService _service;

        public CardsApiController(CardProfileService service)
        {
            _service = service;
        }

        private bool IsAuthorized()
        {
            string expected = Environment.GetEnvironmentVariable("API_KEY") ?? "dbp-api-2026";
            Request.Headers.TryGetValue("X-Api-Key", out var headerKey);
            Request.Query.TryGetValue("apiKey", out var queryKey);
            return headerKey == expected || queryKey == expected;
        }

        private IActionResult Unauthorized401() =>
            StatusCode(401, new { error = "Unauthorized. Provide API key via X-Api-Key header or ?apiKey= query." });

        // GET /api/cards/{empId} — public
        [HttpGet("{empId}")]
        public IActionResult GetCard(string empId)
        {
            var card = _service.Get(empId);
            if (card == null)
                return NotFound(new { error = $"No card found for Employee ID: {empId}" });

            return Ok(new
            {
                empId       = card.EmpId,
                name        = card.Name,
                title       = card.Title,
                org         = card.Org,
                phone       = card.Phone,
                email       = card.Email,
                office      = card.Office,
                github      = card.GitHub,
                linkedin    = card.LinkedIn,
                portfolio   = card.Portfolio,
                hasPhoto    = !string.IsNullOrEmpty(card.Photo),
                cardUrl     = $"{Request.Scheme}://{Request.Host}/card/{card.EmpId}",
                qrUrl       = $"{Request.Scheme}://{Request.Host}/QR/Generate/{card.EmpId}",
                lastUpdated = card.LastUpdated
            });
        }

        // GET /api/cards — requires key
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
                    empId       = card.EmpId,
                    name        = card.Name,
                    title       = card.Title,
                    org         = card.Org,
                    phone       = card.Phone,
                    email       = card.Email,
                    office      = card.Office,
                    cardUrl     = $"{Request.Scheme}://{Request.Host}/card/{card.EmpId}",
                    lastUpdated = card.LastUpdated
                });
            }
            return Ok(new { total = result.Count, cards = result });
        }

        // POST /api/cards — requires key
        [HttpPost]
        public IActionResult UpsertCard([FromBody] CardProfile model)
        {
            if (!IsAuthorized()) return Unauthorized401();
            if (model == null || string.IsNullOrWhiteSpace(model.EmpId))
                return BadRequest(new { error = "empId is required." });
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.EmpId.Trim(), @"^\d{7}-[A-Za-z]{3}$"))
                return BadRequest(new { error = "empId must be 7 digits-3 letters (e.g. 0000001-DBP)" });
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { error = "name is required." });

            _service.Save(model);
            return Ok(new
            {
                success = true,
                message = $"Card for {model.EmpId} saved.",
                cardUrl = $"{Request.Scheme}://{Request.Host}/card/{model.EmpId.Trim()}"
            });
        }

        // DELETE /api/cards/{empId} — requires key
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
