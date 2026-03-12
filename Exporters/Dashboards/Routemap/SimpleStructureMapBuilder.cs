using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    public sealed class SimpleStructureMapBuilder
    {
        public SimpleStructureMapModel Build(
            ProjectStructureResult? structure,
            IReadOnlyCollection<string> namespaces)
        {
            if (structure == null || structure.Lines.Count == 0)
                return new SimpleStructureMapModel(Array.Empty<SimpleStructureNode>());

            var namespaceIndex = BuildNamespaceIndex(namespaces);

            var nodes = new List<SimpleStructureNode>();
            var stack = new Stack<(int Depth, string Id, string Label)>();
            int counter = 0;

            foreach (var rawLine in structure.Lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                var trimmed = SanitizeStructureLine(rawLine);

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (LooksLikeObviousNoise(trimmed))
                    continue;

                var depth = EstimateDepth(rawLine);

                while (stack.Count > 0 && stack.Peek().Depth >= depth)
                    stack.Pop();

                var id = "node_" + (++counter);
                var parentId = stack.Count == 0 ? string.Empty : stack.Peek().Id;

                nodes.Add(new SimpleStructureNode
                {
                    Id = id,
                    Label = trimmed,
                    ParentId = parentId,
                    Depth = depth,
                    CsFileCount = 0
                });

                stack.Push((depth, id, trimmed));
            }

            if (nodes.Count == 0)
                return new SimpleStructureMapModel(Array.Empty<SimpleStructureNode>());

            var filtered = PruneNodesWithoutNamespaceMatch(nodes, namespaceIndex);

            if (filtered.Count == 0)
                return new SimpleStructureMapModel(Array.Empty<SimpleStructureNode>());

            return new SimpleStructureMapModel(RemoveArtificialRootIfNeeded(filtered));
        }

        private static HashSet<string> BuildNamespaceIndex(IReadOnlyCollection<string> namespaces)
        {
            var index = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ns in namespaces)
            {
                if (string.IsNullOrWhiteSpace(ns))
                    continue;

                var parts = ns
                    .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                        index.Add(part);
                }
            }

            return index;
        }

        private static List<SimpleStructureNode> PruneNodesWithoutNamespaceMatch(
            List<SimpleStructureNode> nodes,
            HashSet<string> namespaceIndex)
        {
            var childrenByParent = nodes
                .GroupBy(n => n.ParentId ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(),
                    StringComparer.OrdinalIgnoreCase);

            bool HasNamespaceMatch(SimpleStructureNode node)
            {
                if (namespaceIndex.Contains(node.Label))
                    return true;

                if (!childrenByParent.TryGetValue(node.Id, out var children))
                    return false;

                return children.Any(HasNamespaceMatch);
            }

            return nodes
                .Where(HasNamespaceMatch)
                .ToList();
        }

        private static List<SimpleStructureNode> RemoveArtificialRootIfNeeded(List<SimpleStructureNode> nodes)
        {
            if (nodes.Count == 0)
                return nodes;

            var roots = nodes
                .Where(n => string.IsNullOrWhiteSpace(n.ParentId))
                .ToList();

            if (roots.Count != 1)
                return nodes;

            var root = roots[0];

            var children = nodes
                .Where(n => string.Equals(n.ParentId, root.Id, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (children.Count < 2)
                return nodes;

            var rewritten = new List<SimpleStructureNode>();

            foreach (var node in nodes)
            {
                if (node.Id == root.Id)
                    continue;

                if (string.Equals(node.ParentId, root.Id, StringComparison.OrdinalIgnoreCase))
                {
                    rewritten.Add(new SimpleStructureNode
                    {
                        Id = node.Id,
                        Label = node.Label,
                        ParentId = string.Empty,
                        Depth = Math.Max(0, node.Depth - 1),
                        CsFileCount = node.CsFileCount
                    });
                }
                else
                {
                    rewritten.Add(new SimpleStructureNode
                    {
                        Id = node.Id,
                        Label = node.Label,
                        ParentId = node.ParentId,
                        Depth = Math.Max(0, node.Depth - 1),
                        CsFileCount = node.CsFileCount
                    });
                }
            }

            return rewritten;
        }

        private static int EstimateDepth(string rawLine)
        {
            int depth = 0;

            foreach (var c in rawLine)
            {
                if (c == ' ' || c == '│')
                    depth++;
                else
                    break;
            }

            return depth / 2;
        }

        private static string SanitizeStructureLine(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                return string.Empty;

            var line = rawLine
                .Replace("│", " ")
                .Replace("├", " ")
                .Replace("└", " ")
                .Replace("─", " ")
                .Replace("•", " ")
                .Replace("·", " ")
                .Trim();

            while (line.Contains("  "))
                line = line.Replace("  ", " ");

            return line.Trim();
        }

        private static bool LooksLikeObviousNoise(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;

            return text.Equals("bin", StringComparison.OrdinalIgnoreCase)
                || text.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || text.Equals(".git", StringComparison.OrdinalIgnoreCase)
                || text.Equals("node_modules", StringComparison.OrdinalIgnoreCase)
                || text.Equals("packages", StringComparison.OrdinalIgnoreCase)
                || text.Equals(".vs", StringComparison.OrdinalIgnoreCase)
                || text.Equals(".idea", StringComparison.OrdinalIgnoreCase);
        }
    }
}