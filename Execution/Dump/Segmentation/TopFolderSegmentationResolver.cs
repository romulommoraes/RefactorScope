using RefactorScope.Core.Context;
using RefactorScope.Core.Model;


namespace RefactorScope.Execution.Dump.Segmentation
{
    public class TopFolderSegmentationResolver : ISegmentationResolver
    {
        public IEnumerable<SegmentScope> Resolve(AnalysisContext context)
        {
            var projectDir = FindProjectDirectory(context.Model.RootPath);

            var moduleFolders = projectDir
                .GetDirectories()
                .Where(d => !IsIgnoredFolder(d.Name))
                .Select(d => d.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var groups = context.Model.Tipos
                .GroupBy(t => ResolveModuleFolder(t.DeclaredInFile, moduleFolders));

            foreach (var group in groups)
            {
                var tipos = group.ToList();

                var filteredModel = new ModeloEstrutural(
                    context.Model.RootPath,
                    context.Model.Arquivos.Where(a =>
                        a.Tipos.Any(t => tipos.Contains(t))).ToList(),
                    tipos,
                    context.Model.Referencias.Where(r =>
                        tipos.Any(t => t.Name == r.FromType)).ToList()
                );

                yield return new SegmentScope(
                    group.Key,
                    new AnalysisContext(context.Config, filteredModel)
                );
            }
        }

        private DirectoryInfo FindProjectDirectory(string rootPath)
        {
            var root = new DirectoryInfo(rootPath);

            var projectDir = root
                .GetDirectories("*", SearchOption.AllDirectories)
                .FirstOrDefault(d => d.GetFiles("*.csproj").Any());

            return projectDir ?? root;
        }

        private string ResolveModuleFolder(string relativePath, HashSet<string> modules)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return "Root";

            var parts = relativePath
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (modules.Contains(part))
                    return part;
            }

            return "Root";
        }

        private bool IsIgnoredFolder(string name)
        {
            return name.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || name.Equals(".git", StringComparison.OrdinalIgnoreCase);
        }
    }
}