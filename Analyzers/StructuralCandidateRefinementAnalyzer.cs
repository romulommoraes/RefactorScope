using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Applies probabilistic refinement over Structural Candidates.
    ///
    /// 🔒 ADR-EXP-007:
    /// A type CANNOT remain classified as an Unresolved candidate if:
    /// - It belongs to a known structural category (Analyzer, Result, Exporter, etc.)
    /// - It belongs to infrastructure layers (CLI, Infrastructure, Configuration)
    /// - It is detected through Dependency Injection patterns
    /// - It is detected through interface-based abstractions
    ///
    /// The refinement stage attempts to recognize legitimate architectural patterns
    /// and reclassify those candidates as Pattern Similarity instead of Unresolved.
    ///
    /// IMPORTANT:
    /// This analyzer does NOT modify the Structural Candidate detection stage.
    /// It only evaluates candidates probabilistically and determines whether they
    /// should remain Unresolved or be explained by recognized structural patterns.
    /// </summary>
    public class StructuralCandidateRefinementAnalyzer : IAnalyzer
    {
        public string Name => "zombie-refinement";

        bool IsInfrastructure(TipoInfo tipo)
        {
            return tipo.Name.StartsWith("Exportador")
                || tipo.Name.StartsWith("Aba")
                || tipo.Name.Contains("Dashboard")
                || tipo.Name.Contains("Logger");
        }

        bool IsPlugin(TipoInfo tipo)
        {
            return tipo.Kind == "interface"
                || tipo.Name.StartsWith("Motor")
                || tipo.Name.StartsWith("Classificador");
        }

        bool IsEntryPoint(TipoInfo tipo)
        {
            return tipo.Name == "Program";
        }

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var config = context.Config.StructuralCandidateDetection;

            if (!config.EnableRefinement)
                return new StructuralCandidateProbabilityResult(new List<StructuralCandidateProbabilityItem>());

            var structuralCandidateBase = context.Results
                .OfType<StructuralCandidateResult>()
                .FirstOrDefault();

            if (structuralCandidateBase == null)
                return new StructuralCandidateProbabilityResult(new List<StructuralCandidateProbabilityItem>());

            var totalTypes = context.Model.Tipos.Count;

            var globalStructuralCandidateRate =
                totalTypes == 0
                ? 0
                : (double)structuralCandidateBase.StructuralCandidateTypes.Count / totalTypes;

            var results = new List<StructuralCandidateProbabilityItem>();

            foreach (var typeName in structuralCandidateBase.StructuralCandidateTypes)
            {
                double probability = 1.0;

                bool diDetected = false;
                bool interfaceDetected = false;
                bool structuralProtected = false;

                string confidence = "Alta (estrutural)";

                var tipo = context.Model.Tipos.FirstOrDefault(t => t.Name == typeName);

                // ====================================================
                // 🔒 Proteção arquitetural consolidada
                // ====================================================

                if (tipo != null && IsArchitecturallyProtected(tipo, context))
                {
                    probability = 0.0;
                    structuralProtected = true;
                    confidence = "Protegido por padrão arquitetural";
                }

                // ====================================================
                // 🔎 Refinamento probabilístico
                // ====================================================

                if (!structuralProtected)
                {
                    // Camada 1 – Dependency Injection

                    if (globalStructuralCandidateRate > config.GlobalRateThreshold_DI)
                    {
                        if (IsRegisteredInDI(typeName, context))
                        {
                            probability = config.DIProbability;
                            diDetected = true;
                            confidence = "Provável uso via DI";
                        }
                    }

                    // Camada 2 – Interface naming heuristic

                    if (globalStructuralCandidateRate > config.GlobalRateThreshold_Interface)
                    {
                        if (MatchesInterfacePattern(typeName, context))
                        {
                            probability = config.InterfaceProbability;
                            interfaceDetected = true;
                            confidence = "Provável uso polimórfico";
                        }
                    }
                }

                results.Add(new StructuralCandidateProbabilityItem(
                    typeName,
                    probability,
                    confidence,
                    diDetected,
                    interfaceDetected
                ));
            }

            return new StructuralCandidateProbabilityResult(results);
        }

        // ====================================================
        // 🔒 Proteção estrutural consolidada
        // ====================================================

        private static bool IsStructuralProtected(string typeName, AnalysisContext context)
        {
            if (IsInfrastructureType(typeName))
                return true;

            if (typeName.EndsWith("Analyzer"))
                return true;

            if (typeName.EndsWith("Result"))
                return true;

            if (typeName.EndsWith("Exporter"))
                return true;

            if (typeName.EndsWith("DatasetBuilder"))
                return true;

            if (typeName.EndsWith("Strategy"))
                return true;

            if (typeName.EndsWith("Resolver"))
                return true;

            if (typeName.EndsWith("Config"))
                return true;

            if (typeName.EndsWith("Context"))
                return true;

            if (typeName.EndsWith("Extensions"))
                return true;

            return false;
        }

        // ====================================================
        // 🔎 Heurística DI
        // ====================================================

        private static bool IsRegisteredInDI(string typeName, AnalysisContext context)
        {
            foreach (var arquivo in context.Model.Arquivos)
            {
                var texto = arquivo.SourceCode;

                var patterns = new[]
                {
                    $"AddScoped<{typeName}>(",
                    $"AddTransient<{typeName}>(",
                    $"AddSingleton<{typeName}>(",
                    $"AddScoped(typeof({typeName}))",
                    $"AddTransient(typeof({typeName}))",
                    $"AddSingleton(typeof({typeName}))"
                };

                foreach (var pattern in patterns)
                {
                    if (texto.Contains(pattern))
                    {
                        var index = texto.IndexOf(pattern);

                        var beforeText = texto.Substring(0, index);
                        var quoteCount = beforeText.Count(c => c == '"');

                        if (quoteCount % 2 != 0)
                        {
                            continue;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        // ====================================================
        // 🔎 Interface naming heuristic
        // ====================================================

        private static bool MatchesInterfacePattern(string typeName, AnalysisContext context)
        {
            var interfaceName = $"I{typeName}";

            var interfaceExists =
                context.Model.Tipos
                .Any(t => t.Name == interfaceName && t.Kind == "interface");

            if (!interfaceExists)
                return false;

            var interfaceReferenced =
                context.Model.Tipos
                .Any(t => t.References.Any(r => r.ToType == interfaceName));

            return interfaceReferenced;
        }

        // ====================================================
        // 🔎 Infraestrutura
        // ====================================================

        private static readonly string[] InfrastructureTokens =
        {
            "Extensions",
            "Logger",
            "Validator",
            "Renderer",
            "Loader",
            "Selector",
            "Evaluator",
            "Consolidator",
            "Orchestrator",
            "Discovery"
        };

        private static bool IsInfrastructureType(string typeName)
        {
            foreach (var token in InfrastructureTokens)
            {
                if (typeName.Contains(token))
                    return true;
            }

            return false;
        }

        private bool IsArchitecturallyProtected(TipoInfo tipo, AnalysisContext context)
        {
            if (IsEntryPoint(tipo))
                return true;

            if (IsPlugin(tipo))
                return true;

            if (IsInfrastructure(tipo))
                return true;

            if (IsStructuralProtected(tipo.Name, context))
                return true;

            return false;
        }
    }
}