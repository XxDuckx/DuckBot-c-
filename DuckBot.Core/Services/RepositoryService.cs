using System.Collections.Generic;

namespace DuckBot.Core.Services
{
    public static class RepositoryService
    {
        public record Template(string Name, string Game, string Author, string Url);

        public static List<Template> GetTemplates()
        {
            // TODO: Fetch from GitHub or remote manifest
            return new()
            {
                new Template("Fast Farm", "West Game", "Community", "https://github.com/..."),
                new Template("Build Helper", "West Game", "Brandon", "https://github.com/...")
            };
        }

        public static void Contribute(string localPath)
        {
            // TODO: Upload feature for sharing user-created templates
        }
    }
}
