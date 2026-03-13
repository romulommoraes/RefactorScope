using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Core.Analyzers
{
    public sealed class ArchitecturalHygieneAnalyzer
    {
        /// <summary>
        /// Strict Namespace Alignment:
        /// Namespace must exactly match the folder structure.
        /// </summary>
        public const string NamespaceDriftTooltip =
            "Strict rule: Namespace must match the folder structure under the project root.";

        public HygieneReport Analyze(ConsolidatedReport report)
        {
            var architecture = report.GetResult<ArchitecturalClassificationResult>();
            var isolated = report.GetResult<CoreIsolationResult>();

            if (architecture == null)
                return new HygieneReport(0, 0, 0, 0, 0, 0);

            var items = architecture.Items;
            var total = items.Count;

            var unreferenced = items.Count(i => i.UsageCount == 0);

            var globalNamespace = items.Count(i => i.Namespace == "Global");

            int namespaceDrift = 0;

            foreach (var i in items)
            {
                if (string.IsNullOrWhiteSpace(i.Namespace))
                {
                    namespaceDrift++;
                    continue;
                }

                var folderSource = !string.IsNullOrWhiteSpace(i.FolderHierarchy)
                    ? i.FolderHierarchy
                    : i.Folder;

                if (string.IsNullOrWhiteSpace(folderSource))
                    continue;

                var folderNamespace = NormalizeFolder(folderSource);

                if (!NamespaceMatchesFolder(i.Namespace, folderNamespace))
                    namespaceDrift++;
            }

            var isolatedCoreCount = isolated?.IsolatedCoreTypes.Count ?? 0;

            // Structural Entropy temporarily disabled.
            // Future versions may introduce dependency-based entropy instead of folder distribution.
            const double entropy = 0;

            return new HygieneReport(
                total,
                unreferenced,
                globalNamespace,
                namespaceDrift,
                isolatedCoreCount,
                entropy
            );
        }

        private static bool NamespaceMatchesFolder(string ns, string folderNamespace)
        {
            if (string.IsNullOrWhiteSpace(folderNamespace))
                return true;

            var nsSegments = ns.Split('.');
            var folderSegments = folderNamespace.Split('.');

            if (folderSegments.Length > nsSegments.Length)
                return false;

            for (int i = 1; i <= folderSegments.Length; i++)
            {
                if (!nsSegments[^i].Equals(folderSegments[^i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static string NormalizeFolder(string folder)
        {
            return folder
                .Replace("\\", ".")
                .Replace("/", ".")
                .Trim('.');
        }
    }
}