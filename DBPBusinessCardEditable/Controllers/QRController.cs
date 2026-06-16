using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;
using SkiaSharp;

namespace DBPBusinessCardEditable.Controllers
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
            try
            {
                string url = $"{Request.Scheme}://{Request.Host}/Home/Index";

                // 1 — Generate QR with high error correction (H = 30%)
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

                // 4 — Load the real DBP logo from wwwroot
                string logoPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "dbp-logo.png");

                if (System.IO.File.Exists(logoPath))
                {
                    using SKBitmap logoBitmap = SKBitmap.Decode(logoPath);

                    if (logoBitmap != null)
                    {
                        // 5 — Logo size: 26% of QR, centered
                        int logoSize = (int)(size * 0.26f);
                        int logoX = (size - logoSize) / 2;
                        int logoY = (size - logoSize) / 2;

                        // 6 — White backing square
                        int pad = (int)(size * 0.02f);
                        using (var whitePaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
                        {
                            var rect = new SKRoundRect(
                                new SKRect(logoX - pad, logoY - pad, logoX + logoSize + pad, logoY + logoSize + pad),
                                10, 10);
                            canvas.DrawRoundRect(rect, whitePaint);
                        }

                        // 7 — Draw logo centered
                        var destRect = new SKRect(logoX, logoY, logoX + logoSize, logoY + logoSize);
                        using (var logoPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
                        {
                            canvas.DrawBitmap(logoBitmap, destRect, logoPaint);
                        }
                    }
                }

                // 8 — Return final PNG
                using SKImage finalImage = SKImage.FromBitmap(output);
                using SKData finalData = finalImage.Encode(SKEncodedImageFormat.Png, 100);
                return File(finalData.ToArray(), "image/png");
            }
            catch
            {
                // Fallback: return plain QR without logo if anything fails
                string url = $"{Request.Scheme}://{Request.Host}/Home/Index";
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);
                return File(qrBytes, "image/png");
            }
        }
    }
}
