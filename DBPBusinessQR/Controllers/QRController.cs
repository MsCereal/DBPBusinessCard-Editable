using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QRCoder;

namespace DBPBusinessQR.Controllers
{
    public class QRController : Controller
    {
        // /QR/Show  — display the QR code on screen (use this to scan from your phone)
        public IActionResult Show()
        {
            return View();
        }

        // /QR/Generate  — returns the raw QR PNG image
        public IActionResult Generate()
        {
            // Build an absolute URL using the current request so it works on any host/port
            string url = $"{Request.Scheme}://{Request.Host}/Home/Index";

            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(20);

            return File(qrBytes, "image/png");
        }
    }
}
