using Microsoft.AspNetCore.Mvc;
using QRCoder;
using SkiaSharp;
using System.IO;

namespace DBPBusinessCardEditable.Controllers
{
    public class QRController : Controller
    {
        // GET /QR/Generate/{empId}  — returns QR PNG for a specific employee
        [HttpGet("/QR/Generate/{empId}")]
        public IActionResult Generate(string empId)
        {
            string url = $"{Request.Scheme}://{Request.Host}/card/{empId}";
            return GenerateQrImage(url);
        }

        private IActionResult GenerateQrImage(string url)
        {
            try
            {
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(20);

                using SKBitmap qrBitmap = SKBitmap.Decode(qrBytes);
                if (qrBitmap == null) return File(qrBytes, "image/png");

                int size = qrBitmap.Width;

                using SKBitmap output = new SKBitmap(size, size);
                using SKCanvas canvas = new SKCanvas(output);
                canvas.DrawBitmap(qrBitmap, 0, 0);

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
                // Fallback plain QR
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.H);
                PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
                return File(qrCode.GetGraphic(20), "image/png");
            }
        }
    }
}
