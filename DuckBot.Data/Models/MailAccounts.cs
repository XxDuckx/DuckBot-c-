using System;

namespace DuckBot.Data.Models
{
    public class MailAccount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Label { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool Enabled { get; set; } = true;
    }
}