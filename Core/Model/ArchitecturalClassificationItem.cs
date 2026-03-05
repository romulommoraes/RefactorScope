namespace RefactorScope.Core.Model
{
    public class ArchitecturalClassificationItem
    {
        public string TypeName { get; init; } = string.Empty;
        public string Namespace { get; init; } = string.Empty;

        public string Folder { get; init; } = string.Empty;

        /// <summary>
        /// Caminho completo da hierarquia de pastas relativo ao projeto.
        /// Ex: Core/Abstractions ou Analyzers/Solid/Rules
        /// Usado para análises estruturais profundas.
        /// </summary>
        public string FolderHierarchy { get; init; } = string.Empty;

        public string Layer { get; init; } = string.Empty;
        public string NamespaceAlignment { get; init; } = string.Empty;
        public string StructuralStatus { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string RemovalCandidate { get; init; } = string.Empty;

        public int UsageCount { get; init; }

        public string DeclaredInFile { get; init; } = string.Empty;
    }
}