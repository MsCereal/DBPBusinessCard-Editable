using DBPBusinessCardEditable.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace DBPBusinessCardEditable.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Card()
        {
            return View();
        }

        public IActionResult DownloadContact()
        {
            // VCF format — phone will offer to save this to Contacts with name and number pre-filled
            string vcf =
                "BEGIN:VCARD\r\n" +
                "VERSION:3.0\r\n" +
                "FN:Chelsea De Purificacion\r\n" +
                "N:De Purificacion;Chelsea;;;\r\n" +
                "ORG:Development Bank of the Philippines\r\n" +
                "TITLE:Software Engineer / Cybersecurity Analyst\r\n" +
                "TEL;TYPE=CELL:+639614475634\r\n" +
                "EMAIL:cdepurificacion@dbp.ph\r\n" +
                "URL:https://github.com/MsCereal\r\n" +
                "END:VCARD\r\n";

            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(vcf);

            return File(bytes, "text/vcard", "Chelsea-DBP.vcf");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}