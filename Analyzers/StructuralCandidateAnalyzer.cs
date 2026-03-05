using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Detecta tipos não referenciados por outros tipos no escopo.
    /// </summary>
    public class StructuralCandidateAnalyzer : IAnalyzer
    {
        public string Name => "zombie";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos;

            var tipoNames = tipos
                .Where(IsStructurallyRelevant)
                .Select(t => t.Name)
                .ToHashSet();

            var referencias = context.Model.Referencias;

            var referenced = new HashSet<string>(
                referencias
                    .Where(r => tipoNames.Contains(r.ToType))
                    .Select(r => r.ToType)
            );

            var zombies = tipoNames
                .Where(t => !referenced.Contains(t))
                .ToList();

            return new StructuralCandidateResult(zombies);
        }

        private bool IsEntryPoint(TipoInfo tipo)
        {
            return tipo.Name == "Program"
                || tipo.Name.EndsWith("Startup")
                || tipo.Name == "Main";
        }

        private bool IsStructurallyRelevant(TipoInfo tipo)
        {
            // Entry points
            if (IsEntryPoint(tipo))
                return false;

            // Interfaces não são candidatos estruturais
            if (tipo.Kind == "interface")
                return false;

            // Records costumam ser DTOs
            if (tipo.Kind == "record")
                return false;

            // Enums nunca são zumbis
            if (tipo.Kind == "enum")
                return false;

            // DTO / Model / Contract patterns
            if (tipo.Name.EndsWith("Dto"))
                return false;

            if (tipo.Name.EndsWith("Model"))
                return false;

            if (tipo.Name.EndsWith("Contract"))
                return false;

            if (tipo.Name.EndsWith("Request"))
                return false;

            if (tipo.Name.EndsWith("Response"))
                return false;

            return true;
        }
    }
}