using System.Text.Json;
using RefactorScope.Core.Configuration;

namespace RefactorScope.Infrastructure
{
    public static class ConfigLoader
    {
        public static RefactorScopeConfig Load(string fileName = "refactorscope.json")
        {
            var basePath = AppContext.BaseDirectory;
            var fullPath = Path.Combine(basePath, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Config file not found: {fullPath}");

            var json = File.ReadAllText(fullPath);

            var config = JsonSerializer.Deserialize<RefactorScopeConfig>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (config == null)
                throw new Exception("Invalid configuration file.");

            return config;
        }
    }
}