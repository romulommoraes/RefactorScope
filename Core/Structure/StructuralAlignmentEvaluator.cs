using RefactorScope.Core.Model;

namespace RefactorScope.Core.Structure
{
    public static class StructuralAlignmentEvaluator
    {
        public static string ExtractFolder(string declaredInFile)
        {
            if (string.IsNullOrWhiteSpace(declaredInFile))
                return "Root";

            var parts = declaredInFile
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Ignorar pasta raiz do projeto analisador (ex: AvaliaRoteiro)
            // e capturar a primeira pasta do domínio real (ex: Scriptome)

            if (parts.Length >= 2)
                return parts[1];

            return parts[0];
        }

        public static string EvaluateNamespaceAlignment(string folder, string ns)
        {
            if (string.IsNullOrWhiteSpace(ns))
                return "SemNamespace";

            return ns.Contains(folder, StringComparison.OrdinalIgnoreCase)
                ? "Alinhado"
                : "Desalinhado";
        }

        public static string EvaluateStructuralStatus(
            string folder,
            string ns,
            string layer)
        {
            var namespaceAligned = EvaluateNamespaceAlignment(folder, ns);

            if (namespaceAligned == "Desalinhado")
                return "DriftLogico";

            if (!ns.Contains(layer, StringComparison.OrdinalIgnoreCase))
                return "DriftArquitetural";

            return "Coerente";
        }
    }
}