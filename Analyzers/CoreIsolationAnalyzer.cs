using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;
using RefactorScope.Infrastructure;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Detecta tipos da camada Core que estão estruturalmente isolados.
    ///
    /// Um tipo é considerado isolado quando:
    /// - pertence à camada Core (via layerRules)
    /// - não possui referências de entrada
    /// - não é Entry Point
    ///
    /// A definição de Core é externa (configurável).
    /// </summary>
    public class CoreIsolationAnalyzer : IAnalyzer
    {
        public string Name => "coreIsolation";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            var referenced = referencias
                .Select(r => r.ToType)
                .ToHashSet();

            var entryPoints = tipos
                .Where(t =>
                    LayerRuleEvaluator.ResolveLayer(t, context.Config.LayerRules) == "Infra" ||
                    LayerRuleEvaluator.ResolveLayer(t, context.Config.LayerRules) == "UI")
                .Select(t => t.Name)
                .ToHashSet();

            var isolatedCore = tipos
                .Where(t =>
                    LayerRuleEvaluator.ResolveLayer(t, context.Config.LayerRules) == "Core" &&
                    !referenced.Contains(t.Name) &&
                    !entryPoints.Contains(t.Name))
                .Select(t => t.Name)
                .ToList();

            return new CoreIsolationResult(isolatedCore);
        }
    }
}