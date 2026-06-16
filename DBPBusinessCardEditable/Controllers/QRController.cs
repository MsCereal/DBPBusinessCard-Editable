using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SkiaSharp;
using System;
using System.IO;

namespace DBPBusinessCardEditable.Controllers
{
    public class QRController : Controller
    {
        private const string CookieName = "dbp_user_id";

        // GET /QR/Show  — display the QR code page
        public IActionResult Show()
        {
            return View();
        }

        // GET /QR/Generate  — returns the QR PNG with DBP logo, unique per user
        public IActionResult Generate()
        {
            try
            {
                string userId = GetOrCreateUserId();
                string url = $"{Request.Scheme}://{Request.Host}/card/{userId}";

                // 1 — Generate QR with high error correction
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                // 2 — Load QR bitmap
                using SKBitmap qrBitmap = SKBitmap.Decode(qrBytes);
                if (qrBitmap == null) return File(qrBytes, "image/png");

                int size = qrBitmap.Width;

                // 3 — Create output canvas
                using SKBitmap output = new SKBitmap(size, size);
                using SKCanvas canvas = new SKCanvas(output);
                canvas.DrawBitmap(qrBitmap, 0, 0);

                // 4 — Load the DBP logo from wwwroot
                string logoPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "dbp-logo.png");

                if (System.IO.File.Exists(logoPath))
                {
                    using SKBitmap logoBitmap = SKBitmap.Decode(logoPath);
                    if (logoBitmap != null)
                    {
                        int logoSize = (int)(size * 0.26f);
                        int logoX = (size - logoSize) / 2;
                        int logoY = (size - logoSize) / 2;
                        int pad = (int)(size * 0.02f);

                        using (var whitePaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
                        {
                            var rect = new SKRoundRect(
                                new SKRect(logoX - pad, logoY - pad, logoX + logoSize + pad, logoY + logoSize + pad),
                                10, 10);
                            canvas.DrawRoundRect(rect, whitePaint);
                        }

                        var destRect = new SKRect(logoX, logoY, logoX + logoSize, logoY + logoSize);
                        using (var logoPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
                        {
                            canvas.DrawBitmap(logoBitmap, destRect, logoPaint);
                        }
                    }
                }

                using SKImage finalImage = SKImage.FromBitmap(output);
                using SKData finalData = finalImage.Encode(SKEncodedImageFormat.Png, 100);
                return File(finalData.ToArray(), "image/png");
            }
            catch
            {
                string userId = GetOrCreateUserId();
                string url = $"{Request.Scheme}://{Request.Host}/card/{userId}";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                return File(qrBytes, "image/png");
            }
        }

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
