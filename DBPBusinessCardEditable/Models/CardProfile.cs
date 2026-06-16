using System;

namespace DBPBusinessCardEditable.Models
{
    public class CardProfile
    {
        public string UserId { get; set; }
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public string Org { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string GitHub { get; set; } = "";
        public string LinkedIn { get; set; } = "";
        public string Portfolio { get; set; } = "";
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
