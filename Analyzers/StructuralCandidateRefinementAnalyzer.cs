using System.Text.RegularExpressions;
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
    /// - It is detected through lightweight top-level/bootstrap recovery
    ///
    /// The refinement stage attempts to recognize legitimate architectural patterns
    /// and reclassify those candidates as Pattern Similarity instead of Unresolved.
    ///
    /// IMPORTANT:
    /// This analyzer does NOT modify the Structural Candidate detection stage.
    /// It only evaluates candidates probabilistically and determines whether they
    /// should remain Unresolved or be explained by recognized structural patterns.
    ///
    /// Transitional note
    /// -----------------
    /// The top-level/bootstrap recovery implemented here is intentionally lightweight.
    /// At this stage, it exists as a pragmatic refinement heuristic inside this analyzer.
    ///
    /// Future versions should extract this concern into a dedicated recovery component
    /// (for example: TopLevelRecoveryAnalyzer / BootstrapReferenceRecovery), with:
    /// - clearer provenance
    /// - dedicated result object
    /// - stronger parsing strategy
    /// - reduced false positives
    ///
    /// For the current iteration, this heuristic is acceptable because it solves an
    /// important practical gap: essential types referenced only from Program.cs or
    /// other bootstrap files were being incorrectly preserved as unresolved candidates.
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
                bool topLevelDetected = false;
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
                            probability = Math.Min(probability, config.DIProbability);
                            diDetected = true;
                            confidence = "Provável uso via DI";
                        }
                    }

                    // Camada 2 – Interface naming heuristic
                    if (globalStructuralCandidateRate > config.GlobalRateThreshold_Interface)
                    {
                        if (MatchesInterfacePattern(typeName, context))
                        {
                            probability = Math.Min(probability, config.InterfaceProbability);
                            interfaceDetected = true;
                            confidence = "Provável uso polimórfico";
                        }
                    }

                    // Camada 3 – Top-level / bootstrap lightweight recovery
                    //
                    // Intenção:
                    // ----------
                    // Capturar tipos essenciais que aparecem apenas em Program.cs,
                    // Startup.cs ou arquivos equivalentes de bootstrap e que,
                    // por limitação do parsing estrutural atual, poderiam permanecer
                    // falsamente em Unresolved.
                    //
                    // Estratégia atual:
                    // -----------------
                    // Busca textual com padrões leves, porém mais seguros que um
                    // simples Contains(typeName), tentando reconhecer usos como:
                    //
                    // - new Tipo(...)
                    // - typeof(Tipo)
                    // - AddScoped<Tipo>
                    // - AddTransient<Tipo>
                    // - AddSingleton<Tipo>
                    // - GetService<Tipo>
                    // - GetRequiredService<Tipo>
                    // - referência nominal isolada com boundary
                    //
                    // Observação:
                    // -----------
                    // Esta é uma heurística transitória. No futuro, deve ser
                    // extraída para um recovery dedicado.
                    if (IsReferencedInTopLevelBootstrap(typeName, context))
                    {
                        probability = 0.0;
                        topLevelDetected = true;
                        confidence = "Protegido por referência em Program/Top-Level";
                    }
                }

                results.Add(new StructuralCandidateProbabilityItem(
                    typeName,
                    probability,
                    BuildConfidenceLabel(
                        confidence,
                        diDetected,
                        interfaceDetected,
                        topLevelDetected),
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
                var texto = arquivo.SourceCode ?? string.Empty;

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
                        var index = texto.IndexOf(pattern, StringComparison.Ordinal);

                        if (index < 0)
                            continue;

                        var beforeText = texto.Substring(0, index);
                        var quoteCount = beforeText.Count(c => c == '"');

                        if (quoteCount % 2 != 0)
                            continue;

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
        // 🔎 Top-level / bootstrap lightweight recovery
        // ====================================================

        private static bool IsReferencedInTopLevelBootstrap(string typeName, AnalysisContext context)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return false;

            var bootstrapFiles = context.Model.Arquivos
                .Where(IsBootstrapFile)
                .ToList();

            if (bootstrapFiles.Count == 0)
                return false;

            foreach (var arquivo in bootstrapFiles)
            {
                var source = arquivo.SourceCode ?? string.Empty;

                if (string.IsNullOrWhiteSpace(source))
                    continue;

                var sanitized = SanitizeBootstrapSource(source);

                if (HasSafeTopLevelReference(sanitized, typeName))
                    return true;
            }

            return false;
        }

        private static string SanitizeBootstrapSource(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return string.Empty;

            var text = source;

            // ----------------------------------------------------
            // 1. Remove comentários de bloco: /* ... */
            // ----------------------------------------------------
            text = Regex.Replace(
                text,
                @"/\*.*?\*/",
                " ",
                RegexOptions.Singleline);

            // ----------------------------------------------------
            // 2. Remove comentários de linha: // ...
            // ----------------------------------------------------
            text = Regex.Replace(
                text,
                @"//.*?$",
                " ",
                RegexOptions.Multiline);

            // ----------------------------------------------------
            // 3. Remove strings verbatim/interpoladas verbatim:
            //    @"..."
            //    $@"..."
            //    @$"..."
            // ----------------------------------------------------
            text = Regex.Replace(
                text,
                @"(?<!\w)(\$@|@\$|@)""(?:""""|[^""])*""",
                "\"\"",
                RegexOptions.Singleline);

            // ----------------------------------------------------
            // 4. Remove strings normais/interpoladas:
            //    "..."
            //    $"..."
            // ----------------------------------------------------
            text = Regex.Replace(
                text,
                @"(?<!\w)\$?""(?:\\.|[^""\\])*""",
                "\"\"",
                RegexOptions.Singleline);

            // ----------------------------------------------------
            // 5. Compacta whitespace para facilitar regex posterior
            // ----------------------------------------------------
            text = Regex.Replace(text, @"\s+", " ");

            return text;
        }

        private static bool IsBootstrapFile(ArquivoInfo arquivo)
        {
            var path = TryGetArquivoPath(arquivo);

            if (string.IsNullOrWhiteSpace(path))
                return false;

            var normalized = path.Replace('\\', '/');

            return normalized.EndsWith("/Program.cs", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("/Startup.cs", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("/CompositionRoot.cs", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("/Bootstrapper.cs", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSafeTopLevelReference(string source, string typeName)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(typeName))
                return false;

            var escaped = Regex.Escape(typeName);

            var patterns = new[]
            {
                $@"\b{escaped}\b",
                $@"typeof\s*\(\s*{escaped}\s*\)",
                $@"new\s+{escaped}\s*\(",
                $@"AddScoped\s*<\s*{escaped}\s*>",
                $@"AddTransient\s*<\s*{escaped}\s*>",
                $@"AddSingleton\s*<\s*{escaped}\s*>",
                $@"GetRequiredService\s*<\s*{escaped}\s*>",
                $@"GetService\s*<\s*{escaped}\s*>"
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(source, pattern))
                    return true;
            }

            return false;
        }

        private static string? TryGetArquivoPath(ArquivoInfo arquivo)
        {
            try
            {
                var type = arquivo.GetType();

                var prop =
                    type.GetProperty("Path")
                    ?? type.GetProperty("FilePath")
                    ?? type.GetProperty("FullPath")
                    ?? type.GetProperty("RelativePath")
                    ?? type.GetProperty("Nome")
                    ?? type.GetProperty("Name");

                return prop?.GetValue(arquivo)?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string BuildConfidenceLabel(
            string baseLabel,
            bool diDetected,
            bool interfaceDetected,
            bool topLevelDetected)
        {
            var tags = new List<string>();

            if (diDetected)
                tags.Add("DI");

            if (interfaceDetected)
                tags.Add("Interface");

            if (topLevelDetected)
                tags.Add("Top-Level");

            if (tags.Count == 0)
                return baseLabel;

            return $"{baseLabel} [{string.Join(" / ", tags)}]";
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