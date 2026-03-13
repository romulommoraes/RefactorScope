using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Projections.Architecture
{
    public sealed class ArchitectureVisualizationResolver
    {
        /// <summary>
        /// Compatibilidade com o fluxo atual.
        /// Resolve apenas o modo inferido.
        /// </summary>
        public ArchitectureVisualizationMode Resolve(ConsolidatedReport report)
        {
            return ResolveInferredMode(report);
        }

        /// <summary>
        /// Novo ponto principal de resolução:
        /// 1) decide se usa versão curada
        /// 2) caso contrário, escolhe o tipo inferido
        /// </summary>
        public ArchitectureVisualizationDecision ResolveDecision(ConsolidatedReport report)
        {
            if (ShouldUseCuratedRefactorScope(report))
                return ArchitectureVisualizationDecision.CuratedRefactorScope();

            var inferredMode = ResolveInferredMode(report);
            return ArchitectureVisualizationDecision.Inferred(inferredMode);
        }

        private ArchitectureVisualizationMode ResolveInferredMode(ConsolidatedReport report)
        {
            var routeModel = TryBuildRouteModel(report);
            var structure = report.GetResult<ProjectStructureResult>();

            if (LooksConventional(routeModel))
                return ArchitectureVisualizationMode.ConventionalFlow;

            if (LooksModular(report, routeModel, structure))
                return ArchitectureVisualizationMode.ModularExchange;

            return ArchitectureVisualizationMode.SimpleStructure;
        }

        private static bool ShouldUseCuratedRefactorScope(ConsolidatedReport report)
        {
            if (string.IsNullOrWhiteSpace(report.TargetScope))
                return false;

            var trimmed = report.TargetScope.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var rootName = Path.GetFileName(trimmed);

            return !string.IsNullOrWhiteSpace(rootName)
                && rootName.Equals("RefactorScope", StringComparison.OrdinalIgnoreCase);
        }

        private static ModuleRouteMapModel? TryBuildRouteModel(ConsolidatedReport report)
        {
            var builder = new ModuleRouteMapBuilder();
            var model = builder.Build(report);

            if (model.Nodes.Count == 0)
                return null;

            return model;
        }

        private static bool LooksConventional(ModuleRouteMapModel? model)
        {
            if (model == null)
                return false;

            var stationCount = model.Nodes.Count(n => n.Kind == "station");
            var hubCount = model.Nodes.Count(n => n.Kind == "hub");
            var exitCount = model.Nodes.Count(n => n.Kind == "exit");
            var processCount = model.Nodes.Count(n => n.Kind == "process");

            return stationCount >= 2
                && hubCount >= 1
                && exitCount >= 1
                && processCount >= 2;
        }

        private static bool LooksModular(
            ConsolidatedReport report,
            ModuleRouteMapModel? model,
            ProjectStructureResult? structure)
        {
            int score = 0;

            if (model != null)
            {
                var stationCount = model.Nodes.Count(n => n.Kind == "station");
                var processCount = model.Nodes.Count(n => n.Kind == "process");
                var supportCount = model.Nodes.Count(n => n.Kind == "support");
                var hubCount = model.Nodes.Count(n => n.Kind == "hub");

                if (stationCount <= 1 && processCount >= 3)
                    score += 2;

                if (supportCount >= 1)
                    score += 1;

                if (hubCount == 1)
                    score += 1;
            }

            if (structure != null)
            {
                var joined = string.Join("\n", structure.Lines);

                if (ContainsAny(joined, "RaizComposicao", "RaizDeComposicao", "RootComposition", "CompositionRoot"))
                    score += 3;

                if (ContainsAny(joined, "Orquestrador", "Orchestrator"))
                    score += 2;

                if (ContainsAny(joined, "Strategy", "Engine", "Adapter", "Provider"))
                    score += 2;

                if (ContainsAny(joined, "Modulo", "Módulo", "Module"))
                    score += 1;

                if (ContainsAny(joined, "Resultado", "Result", "Contexto", "Context"))
                    score += 1;
            }

            return score >= 4;
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var term in terms)
            {
                if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}