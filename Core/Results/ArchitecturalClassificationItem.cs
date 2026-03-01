using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Core.Results
{
    public class ArchitecturalClassificationItem
    {
        public string TypeName { get; init; } = string.Empty;
        public string Namespace { get; init; } = string.Empty;
        public string Folder { get; init; } = string.Empty;
        public string Layer { get; init; } = string.Empty;
        public string NamespaceAlignment { get; init; } = string.Empty;
        public string StructuralStatus { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string RemovalCandidate { get; init; } = string.Empty;
        public int UsageCount { get; init; }

        public string DeclaredInFile { get; init; }
    }
}
