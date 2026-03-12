using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using System.Text;

namespace RefactorScope.Exporters.Reports
{
    /// <summary>
    /// Exporta a árvore limpa do projeto.
    ///
    /// Finalidade
    /// ----------
    /// Fornecer contexto estrutural textual para:
    /// - debugging
    /// - auditoria
    /// - suporte a IA
    /// - inspeção rápida da topologia do projeto
    ///
    /// Convenção atual de saída
    /// ------------------------
    /// output/dumps/structure/ProjectTree.md
    ///
    /// Observação
    /// ----------
    /// Este artefato não é tratado como dataset tabular.
    /// Ele pertence ao bloco de dumps/contexto estrutural.
    /// </summary>
    public sealed class ProjectStructureExporter : IExporter
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

            var ignoredNames = BuildIgnoredNames(context);

            WriteDirectory(
                builder,
                root,
                indent: "",
                ignoredNames,
                isRoot: true);

            var rootOutputPath = context.Config.OutputPath;
            var dumpsRootPath = Path.Combine(rootOutputPath, "dumps");
            var structureDirectory = Path.Combine(dumpsRootPath, "structure");

            Directory.CreateDirectory(structureDirectory);

            var path = Path.Combine(structureDirectory, "ProjectTree.md");

            File.WriteAllText(path, builder.ToString());
        }

        private void WriteDirectory(
            StringBuilder builder,
            string path,
            string indent,
            HashSet<string> ignoredNames,
            bool isRoot = false)
        {
            var dir = new DirectoryInfo(path);

            if (!dir.Exists)
                return;

            if (!isRoot)
                builder.AppendLine($"{indent}├── {dir.Name}");

            var subDirs = dir.GetDirectories()
                .Where(d => !IsIgnored(d.Name, ignoredNames))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var sub in subDirs)
            {
                WriteDirectory(
                    builder,
                    sub.FullName,
                    indent + "│   ",
                    ignoredNames);
            }
        }

        private static HashSet<string> BuildIgnoredNames(AnalysisContext context)
        {
            var ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "bin",
                "obj",
                ".git",
                ".vs",
                "Batch",
                "refactorscope-output",
                "refactorscope-self-analysis"
            };

            if (context?.Config?.Exclude != null)
            {
                foreach (var item in context.Config.Exclude)
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;

                    var normalized = item
                        .Replace('/', Path.DirectorySeparatorChar)
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Trim();

                    if (string.IsNullOrWhiteSpace(normalized))
                        continue;

                    var fileName = Path.GetFileName(normalized.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar));

                    if (!string.IsNullOrWhiteSpace(fileName))
                        ignored.Add(fileName);
                }
            }

            return ignored;
        }

        private static bool IsIgnored(string name, HashSet<string> ignoredNames)
        {
            if (string.IsNullOrWhiteSpace(name))
                return true;

            return ignoredNames.Contains(name);
        }
    }
}