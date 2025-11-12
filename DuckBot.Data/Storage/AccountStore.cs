using DuckBot.Data.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DuckBot.Data.Storage
{
    public static class AccountStore
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        public static string AccountsPath
            => Path.Combine(AppContext.BaseDirectory, "data", "accounts", "accounts.json");

        public static List<MailAccount> LoadAll()
        {
            string path = AccountsPath;
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, "[]");
                return new List<MailAccount>();
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<MailAccount>>(json, Options) ?? new List<MailAccount>();
            }
            catch
            {
                return new List<MailAccount>();
            }
        }

        public static void SaveAll(IEnumerable<MailAccount> accounts)
        {
            string path = AccountsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(accounts, Options);
            File.WriteAllText(path, json);
        }
    }
}