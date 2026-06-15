using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;
using SkiaSharp;

namespace DBPBusinessQR.Controllers
{
    public class QRController : Controller
    {
        // /QR/Show  — display the QR code on screen (use this to scan from your phone)
        public IActionResult Show()
        {
            return View();
        }

        // /QR/Generate  — returns the QR PNG with DBP logo in the center
        public IActionResult Generate()
        {
            string url = $"{Request.Scheme}://{Request.Host}/Home/Index";

            // 1 — Generate QR with high error correction (H = 30%) so logo doesn't break scanning
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            // 2 — Load QR bitmap
            using SKBitmap qrBitmap = SKBitmap.Decode(qrBytes);
            int size = qrBitmap.Width;

            // 3 — Create output canvas
            using SKBitmap output = new SKBitmap(size, size);
            using SKCanvas canvas = new SKCanvas(output);
            canvas.DrawBitmap(qrBitmap, 0, 0);

            // 4 — Load the real DBP logo from wwwroot
            string logoPath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot", "dbp-logo.png");

            using SKBitmap logoBitmap = SKBitmap.Decode(logoPath);

            // 5 — Logo size: 26% of QR, centered
            int logoSize = (int)(size * 0.26f);
            int logoX = (size - logoSize) / 2;
            int logoY = (size - logoSize) / 2;

            // 6 — White rounded backing square for clean edge (slightly larger)
            int pad = (int)(size * 0.02f);
            using (var whitePaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
            {
                var rect = new SKRoundRect(
                    new SKRect(logoX - pad, logoY - pad, logoX + logoSize + pad, logoY + logoSize + pad),
                    10, 10);
                canvas.DrawRoundRect(rect, whitePaint);
            }

            // 7 — Draw the logo scaled into the center
            var destRect = new SKRect(logoX, logoY, logoX + logoSize, logoY + logoSize);
            using (var logoPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
            {
                canvas.DrawBitmap(logoBitmap, destRect, logoPaint);
            }

            // 8 — Return final PNG
            using SKImage finalImage = SKImage.FromBitmap(output);
            using SKData finalData = finalImage.Encode(SKEncodedImageFormat.Png, 100);
            return File(finalData.ToArray(), "image/png");
        }
    }
}
