using System;

namespace DBPBusinessCardEditable.Models
{
    public class CardProfile
    {
        public string EmpId     { get; set; } = "";
        public string Name      { get; set; } = "";
        public string Title     { get; set; } = "";
        public string Org       { get; set; } = "Development Bank of the Philippines";
        public string Phone     { get; set; } = "";
        public string Email     { get; set; } = "";
        public string GitHub    { get; set; } = "";
        public string LinkedIn  { get; set; } = "";
        public string Portfolio { get; set; } = "";
        public string PhotoUrl  { get; set; } = "";   // optional profile photo URL
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
