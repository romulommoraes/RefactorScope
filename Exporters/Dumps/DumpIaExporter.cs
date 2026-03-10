using System.Text.Json;
using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Dumps
{
    public sealed class DumpIaExporter : IExporter
    {
        public string Name => "dumpIA";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var root = context.Config.RootPath;

            var tipos = context.Model.Tipos.Select(t =>
            {
                var fullPath = Path.Combine(root, t.DeclaredInFile);

                var code = string.Empty;

                if (File.Exists(fullPath))
                    code = File.ReadAllText(fullPath);

                return new
                {
                    t.Name,
                    t.Namespace,
                    File = t.DeclaredInFile,
                    Code = code
                };
            });

            var output = new
            {
                RootPath = root,
                Tipos = tipos,
                Referencias = context.Model.Referencias
            };

            var json = JsonSerializer.Serialize(
                output,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            var rootOutputPath = context.Config.OutputPath;
            var dumpsRoot = Path.Combine(rootOutputPath, "dumps");
            var moduleDirectory = Path.Combine(dumpsRoot, "ia");

            Directory.CreateDirectory(moduleDirectory);

            var path = Path.Combine(moduleDirectory, "RefactorScope_DumpIA.json");

            File.WriteAllText(path, json);
        }
    }
}