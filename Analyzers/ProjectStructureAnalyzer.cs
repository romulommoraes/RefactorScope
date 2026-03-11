using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Analisa a estrutura de diretórios do escopo e produz uma árvore limpa
    /// focada no projeto principal.
    /// </summary>
    public class ProjectStructureAnalyzer : IAnalyzer
    {
        public string Name => "project-structure";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var scanRoot = context.Config.RootPath;

            if (string.IsNullOrWhiteSpace(scanRoot) || !Directory.Exists(scanRoot))
                return new ProjectStructureResult(new List<string>());

            var resolvedRoot = ResolveProjectRoot(scanRoot);

            var lines = new List<string>();
            WriteProjectTree(lines, resolvedRoot);

            return new ProjectStructureResult(lines);
        }

        private DirectoryInfo ResolveProjectRoot(string scanRoot)
        {
            var rootDir = new DirectoryInfo(scanRoot);

            if (ContainsPrimaryProjectSignal(rootDir))
                return rootDir;

            var directChildren = rootDir.GetDirectories()
                .Where(d => !IsIgnoredTopLevel(d.Name))
                .ToList();

            if (directChildren.Count == 0)
                return rootDir;

            var best = directChildren
                .Select(d => new
                {
                    Dir = d,
                    Score = ScoreDirectoryAsProjectRoot(d, rootDir.Name)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Dir.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            return best != null && best.Score > 0
                ? best.Dir
                : rootDir;
        }

        private int ScoreDirectoryAsProjectRoot(DirectoryInfo dir, string solutionLikeName)
        {
            int score = 0;
            var name = dir.Name;

            if (ContainsPrimaryProjectSignal(dir))
                score += 100;

            if (name.Equals(solutionLikeName, StringComparison.OrdinalIgnoreCase))
                score += 80;

            if (name.StartsWith(solutionLikeName + ".", StringComparison.OrdinalIgnoreCase))
                score += 20;

            if (LooksLikeAuxiliaryProject(name))
                score -= 120;

            if (LooksLikeDocumentation(name))
                score -= 140;

            if (LooksLikeInfraNoise(name))
                score -= 200;

            int subdirCount = 0;
            try
            {
                subdirCount = dir.GetDirectories()
                    .Count(d => !IsIgnored(d.Name));
            }
            catch
            {
                subdirCount = 0;
            }

            score += Math.Min(subdirCount, 12);

            return score;
        }

        private bool ContainsPrimaryProjectSignal(DirectoryInfo dir)
        {
            try
            {
                var csprojs = dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);

                if (csprojs.Length == 0)
                    return false;

                return csprojs.Any(f =>
                    !LooksLikeAuxiliaryProject(Path.GetFileNameWithoutExtension(f.Name)));
            }
            catch
            {
                return false;
            }
        }

        private void WriteProjectTree(List<string> lines, DirectoryInfo root)
        {
            lines.Add($"└── {root.Name}");

            var subDirs = root.GetDirectories()
                .Where(d => !IsIgnored(d.Name))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < subDirs.Count; i++)
            {
                var isLast = i == subDirs.Count - 1;
                WriteDirectory(lines, subDirs[i], "", isLast);
            }
        }

        private void WriteDirectory(List<string> lines, DirectoryInfo dir, string indent, bool isLast)
        {
            var branch = isLast ? "└── " : "├── ";
            lines.Add($"{indent}{branch}{dir.Name}");

            var childIndent = indent + (isLast ? "    " : "│   ");

            var subDirs = dir.GetDirectories()
                .Where(d => !IsIgnored(d.Name))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            for (int i = 0; i < subDirs.Count; i++)
            {
                WriteDirectory(lines, subDirs[i], childIndent, i == subDirs.Count - 1);
            }
        }

        private bool IsIgnored(string name)
        {
            if (LooksLikeInfraNoise(name))
                return true;

            if (LooksLikeDocumentation(name))
                return true;

            if (LooksLikeAuxiliaryProject(name))
                return true;

            return false;
        }

        private bool IsIgnoredTopLevel(string name)
        {
            if (LooksLikeInfraNoise(name))
                return true;

            if (LooksLikeDocumentation(name))
                return true;

            return false;
        }

        private bool LooksLikeAuxiliaryProject(string name)
        {
            return name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Benchmarks", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Benchmark", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Samples", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Sample", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Examples", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".Example", StringComparison.OrdinalIgnoreCase);
        }

        private bool LooksLikeDocumentation(string name)
        {
            return name.Equals("Docs", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Doc", StringComparison.OrdinalIgnoreCase)
                || name.Equals("Documentation", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".github", StringComparison.OrdinalIgnoreCase);
        }

        private bool LooksLikeInfraNoise(string name)
        {
            return name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".vs", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".idea", StringComparison.OrdinalIgnoreCase)
                || name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                || name.Equals("packages", StringComparison.OrdinalIgnoreCase)
                || name.Equals("TestResults", StringComparison.OrdinalIgnoreCase);
        }
    }
}