using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Aplica refinamento probabilístico sobre candidatos estruturais a Zombie.
    ///
    /// 🔒 ADR-EXP-007:
    /// Um tipo NÃO pode ser confirmado como Zombie se:
    /// - Pertencer a categoria estrutural conhecida (Analyzer, Result, Exporter, etc.)
    /// - For detectado via DI
    /// - For detectado via interface
    ///
    /// Esta classe NÃO altera Structural Candidates.
    /// Apenas decide confirmação probabilística.
    /// </summary>
    public class ZombieRefinementAnalyzer : IAnalyzer
    {
        public string Name => "zombie-refinement";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var config = context.Config.ZombieDetection;

            if (!config.EnableRefinement)
                return new ZombieProbabilityResult(new List<ZombieProbabilityItem>());

            var zombieBase = context.Results
                .OfType<ZombieResult>()
                .FirstOrDefault();

            if (zombieBase == null)
                return new ZombieProbabilityResult(new List<ZombieProbabilityItem>());

            var totalTypes = context.Model.Tipos.Count;
            var globalZombieRate = totalTypes == 0
                ? 0
                : (double)zombieBase.ZombieTypes.Count / totalTypes;

            var results = new List<ZombieProbabilityItem>();

            foreach (var typeName in zombieBase.ZombieTypes)
            {
                double probability = 1.0;
                bool diDetected = false;
                bool interfaceDetected = false;
                bool structuralProtected = false;
                string confidence = "Alta (estrutural)";

                // ====================================================
                // 🔒 Camada 0 – Proteção de Categoria Estrutural
                // ====================================================
                if (IsStructuralProtected(typeName))
                {
                    probability = 0.0;
                    structuralProtected = true;
                    confidence = "Categoria estrutural protegida (ADR-007)";
                }
                else
                {
                    // ===============================
                    // Camada 1 – DI
                    // ===============================
                    if (globalZombieRate > config.GlobalRateThreshold_DI)
                    {
                        if (IsRegisteredInDI(typeName, context))
                        {
                            probability = config.DIProbability;
                            diDetected = true;
                            confidence = "Provável uso via DI";
                        }
                    }

                    // ===============================
                    // Camada 2 – Interface naming heuristic
                    // ===============================
                    if (globalZombieRate > config.GlobalRateThreshold_Interface)
                    {
                        if (MatchesInterfacePattern(typeName, context))
                        {
                            probability = config.InterfaceProbability;
                            interfaceDetected = true;
                            confidence = "Provável uso polimórfico";
                        }
                    }
                }

                results.Add(new ZombieProbabilityItem(
                    typeName,
                    probability,
                    confidence,
                    diDetected,
                    interfaceDetected
                ));
            }

            Console.WriteLine("[DEBUG] ZombieRefinementAnalyzer executado");
            Console.WriteLine($"[DEBUG] Base zombies: {zombieBase.ZombieTypes.Count}");
            Console.WriteLine($"[DEBUG] Global zombie rate: {globalZombieRate:0.00}");
            Console.WriteLine($"[DEBUG] Confirmed after refinement: {results.Count(r => r.Probability >= config.MinZombieProbabilityThreshold)}");

            return new ZombieProbabilityResult(results);
        }

        // ====================================================
        // 🔒 Filtro de categorias estruturais protegidas
        // ====================================================
        private bool IsStructuralProtected(string typeName)
        {
            return typeName.EndsWith("Analyzer")
                || typeName.EndsWith("Result")
                || typeName.EndsWith("Exporter")
                || typeName.EndsWith("DatasetBuilder")
                || typeName.EndsWith("Strategy")
                || typeName.EndsWith("Resolver")
                || typeName.EndsWith("Config")
                || typeName.EndsWith("Context");
        }

        // ====================================================
        // 🔎 Heurística DI (SourceCode real)
        // ====================================================
        private bool IsRegisteredInDI(string typeName, AnalysisContext context)
        {
            foreach (var arquivo in context.Model.Arquivos)
            {
                var texto = arquivo.SourceCode;

                if (texto.Contains($"AddScoped<{typeName}>") ||
                    texto.Contains($"AddTransient<{typeName}>") ||
                    texto.Contains($"AddSingleton<{typeName}>"))
                    return true;
            }

            return false;
        }

        // ====================================================
        // 🔎 Heurística Interface baseada em convenção INome
        // ====================================================
        private bool MatchesInterfacePattern(string typeName, AnalysisContext context)
        {
            var interfaceName = $"I{typeName}";

            var interfaceExists = context.Model.Tipos
                .Any(t => t.Name == interfaceName && t.Kind == "interface");

            if (!interfaceExists)
                return false;

            var interfaceReferenced = context.Model.Tipos
                .Any(t => t.References.Any(r => r.ToType == interfaceName));

            return interfaceReferenced;
        }
    }
}