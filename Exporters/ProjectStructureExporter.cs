using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Orchestration;
using System.Text;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Exporta a árvore limpa do projeto.
    /// Útil como contexto estrutural para IA.
    /// </summary>
    public class ProjectStructureExporter : IExporter
    {
        public string Name => "projectTree";

        public void Export(
            AnalysisContext context,
            ConsolidatedReport report,
            string outputPath)
        {
            var root = context.Config.RootPath;
            var builder = new StringBuilder();

            builder.AppendLine("# Project Structure");
            builder.AppendLine();

            WriteDirectory(builder, root, "", true);

            var path = Path.Combine(outputPath, "ProjectTree.md");

            File.WriteAllText(path, builder.ToString());
        }

        private void WriteDirectory(StringBuilder builder, string path, string indent, bool isRoot = false)
        {
            var dir = new DirectoryInfo(path);

            if (!isRoot)
                builder.AppendLine($"{indent}├── {dir.Name}");

            var subDirs = dir.GetDirectories()
                .Where(d => !IsIgnored(d.Name))
                .OrderBy(d => d.Name);

            foreach (var sub in subDirs)
            {
                WriteDirectory(builder, sub.FullName, indent + "│   ");
            }
        }

        private bool IsIgnored(string name)
        {
            return name switch
            {
                "bin" => true,
                "obj" => true,
                ".git" => true,
                ".vs" => true,
                _ => false
            };
        }
    }
}