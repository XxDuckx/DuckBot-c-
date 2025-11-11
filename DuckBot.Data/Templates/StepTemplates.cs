namespace DuckBot.Data.Templates
{
    public static class StepTemplates
    {
        public static readonly Dictionary<string, Dictionary<string, object>> Library = new()
        {
            { "TAP", new() { { "x", 0 }, { "y", 0 }, { "delay", 500 } } },
            { "WAIT", new() { { "delay", 1000 } } },
            { "INPUT", new() { { "text", "" } } },
            { "IF_IMAGE", new() { { "imagePath", "" }, { "confidence", 0.9f } } },
            { "LOG", new() { { "message", "" } } },
            { "LOOP", new() { { "count", 1 } } },
            { "ENDLOOP", new() },
            { "CUSTOM_JS", new() { { "code", "" } } }
        };

        // TODO: Load external step templates per game from disk (e.g., /Games/{game}/templates/)
    }
}
