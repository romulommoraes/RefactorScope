using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Context;
using RefactorScope.Core.Patterns;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Avalia prontidão arquitetural para CI/CD.
    ///
    /// Versão 1.0:
    /// - Baseado exclusivamente em Structural Unreferenced.
    /// - Não depende de refinamento probabilístico.
    /// - Linguagem alinhada com Dashboard e ADR-EXP-008.
    /// </summary>
    public class FitnessGateAnalyzer : IAnalyzer
    {
        public string Name => "fitnessGates";

        private readonly FitnessGateConfig _config;

        public FitnessGateAnalyzer(FitnessGateConfig config)
        {
            _config = config;
        }

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var gates = new List<FitnessGateStatus>();

            var architecture =
               context.GetResult<ArchitecturalClassificationResult>();

            int totalTypes = context.Model.Tipos.Count;

            int pureUnreferenced = 0;

            if (architecture != null)
            {
                // Mapeia usage por nome
                var usageMap = architecture.Items
                    .ToDictionary(
                        i => $"{i.Namespace}.{i.TypeName}",
                        i => i.UsageCount
                    );

                foreach (var tipo in context.Model.Tipos)
                {
                    if (!usageMap.TryGetValue(tipo.Name, out var usage))
                        continue;

                    if (usage > 0)
                        continue;

                    var pattern =
                        DesignPatternSignatureLibrary.Evaluate(tipo);

                    if (!pattern.IsMatch)
                        pureUnreferenced++;
                }
            }

            var unrefRate = totalTypes == 0
                ? 0
                : pureUnreferenced / (double)totalTypes;

            // ===============================
            // 🔹 Gate: Structural Unreferenced
            // ===============================

            if (unrefRate >= _config.DeadCode.FailAbove)
            {
                gates.Add(new FitnessGateStatus
                {
                    GateName = "UnreferencedTypes",
                    Status = GateStatus.Fail,
                    Message = $"Structural Unreferenced rate alto: {unrefRate:P0}"
                });
            }
            else if (unrefRate >= _config.DeadCode.WarnAbove)
            {
                gates.Add(new FitnessGateStatus
                {
                    GateName = "UnreferencedTypes",
                    Status = GateStatus.Warn,
                    Message = $"Structural Unreferenced rate elevado: {unrefRate:P0}"
                });
            }
            else
            {
                gates.Add(new FitnessGateStatus
                {
                    GateName = "UnreferencedTypes",
                    Status = GateStatus.Pass,
                    Message = $"Structural Unreferenced controlado: {unrefRate:P0}"
                });
            }

            return new FitnessGateResult(gates);
        }
    }
}