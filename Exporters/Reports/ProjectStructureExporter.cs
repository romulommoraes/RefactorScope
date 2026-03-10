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

            WriteDirectory(builder, root, "", true);

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
            bool isRoot = false)
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