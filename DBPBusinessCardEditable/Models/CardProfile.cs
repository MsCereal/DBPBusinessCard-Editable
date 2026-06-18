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
        public string Photo     { get; set; } = "";   // base64 data URI or "avatar:male" / "avatar:female"
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
