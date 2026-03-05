using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Analisa a estrutura de diretórios do projeto
    /// e produz um DTO com a árvore limpa.
    /// </summary>
    public class ProjectStructureAnalyzer : IAnalyzer
    {
        public string Name => "project-structure";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var root = context.Config.RootPath;

            var lines = new List<string>();

            WriteDirectory(lines, root, "", true);

            return new ProjectStructureResult(lines);
        }

        private void WriteDirectory(List<string> lines, string path, string indent, bool isRoot = false)
        {
            var dir = new DirectoryInfo(path);

            if (!isRoot)
                lines.Add($"{indent}├── {dir.Name}");

            var subDirs = dir.GetDirectories()
                .Where(d => !IsIgnored(d.Name))
                .OrderBy(d => d.Name);

            foreach (var sub in subDirs)
            {
                WriteDirectory(lines, sub.FullName, indent + "│   ");
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