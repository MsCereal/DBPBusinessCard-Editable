using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

            // 1 — Generate raw QR bytes using QRCoder (ECCLevel H = 30% error correction so logo doesn't break it)
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            // 2 — Load QR into SkiaSharp bitmap
            using SKBitmap qrBitmap = SKBitmap.Decode(qrBytes);
            int size = qrBitmap.Width;

            // 3 — Create canvas to draw on
            using SKBitmap output = new SKBitmap(size, size);
            using SKCanvas canvas = new SKCanvas(output);
            canvas.DrawBitmap(qrBitmap, 0, 0);

            // 4 — Logo dimensions: ~22% of QR size centered
            int logoSize = (int)(size * 0.22f);
            int logoX = (size - logoSize) / 2;
            int logoY = (size - logoSize) / 2;
            float cx = size / 2f;
            float cy = size / 2f;
            float radius = logoSize / 2f;

            // 5 — White backing circle (slightly larger for padding)
            using (var whitePaint = new SKPaint { IsAntialias = true, Color = SKColors.White })
            {
                canvas.DrawCircle(cx, cy, radius + 6, whitePaint);
            }

            // 6 — Gradient circle: blue (#2563eb) to red (#dc2626)
            using (var gradientPaint = new SKPaint { IsAntialias = true })
            {
                gradientPaint.Shader = SKShader.CreateLinearGradient(
                    new SKPoint(logoX, logoY),
                    new SKPoint(logoX + logoSize, logoY + logoSize),
                    new SKColor[] { new SKColor(0x25, 0x63, 0xeb), new SKColor(0xdc, 0x26, 0x26) },
                    null,
                    SKShaderTileMode.Clamp
                );
                canvas.DrawCircle(cx, cy, radius, gradientPaint);
            }

            // 7 — "DBP" text in white centered on the circle
            using (var textPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.White,
                TextAlign = SKTextAlign.Center,
                FakeBoldText = true,
                TextSize = logoSize * 0.36f
            })
            {
                // Center text vertically
                SKRect textBounds = new SKRect();
                textPaint.MeasureText("DBP", ref textBounds);
                float textY = cy - textBounds.MidY;
                canvas.DrawText("DBP", cx, textY, textPaint);
            }

            // 8 — Encode final image as PNG and return
            using SKImage finalImage = SKImage.FromBitmap(output);
            using SKData finalData = finalImage.Encode(SKEncodedImageFormat.Png, 100);
            return File(finalData.ToArray(), "image/png");
        }
    }
}
