using System;
using System.Collections.Generic;
using System.Linq;
using RefactorScope.Analyzers.Solid;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Exporters.Dashboards.RouteMap
{
    /// <summary>
    /// Constrói um mapa estrutural de rotas entre módulos a partir
    /// dos resultados já calculados pelo pipeline.
    ///
    /// Esta versão é deliberadamente heurística e honesta:
    /// - infere entradas
    /// - infere hubs
    /// - infere saídas
    /// - consolida arestas por módulo
    ///
    /// Não tenta afirmar trajeto exato de runtime.
    /// </summary>
    public sealed class ModuleRouteMapBuilder
    {
        public ModuleRouteMapModel Build(ConsolidatedReport report)
        {
            var coupling = report.GetResult<CouplingResult>();
            if (coupling == null)
                return new ModuleRouteMapModel(
                    Array.Empty<ModuleRouteNode>(),
                    Array.Empty<ModuleRouteEdge>());

            var entryPoints = report.GetResult<EntryPointHeuristicResult>();
            var implicitCoupling = report.GetResult<ImplicitCouplingResult>();
            var solid = report.GetResult<SolidResult>();
            var architectural = report.GetResult<ArchitecturalClassificationResult>();

            var knownModules = CollectKnownModules(coupling, architectural);

            var rawEdges = BuildEdges(coupling, knownModules);
            var mergedEdges = MergeBidirectionalEdges(rawEdges);
            var nodes = BuildNodes(
                knownModules,
                mergedEdges,
                entryPoints,
                implicitCoupling,
                solid);

            ComputeNodeMetrics(nodes, mergedEdges);

            return new ModuleRouteMapModel(nodes, mergedEdges);
        }

        private static List<string> CollectKnownModules(
            CouplingResult coupling,
            ArchitecturalClassificationResult? architectural)
        {
            var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var m in coupling.ModuleFanOut.Keys)
                modules.Add(NormalizeModuleName(m));

            foreach (var m in coupling.TypeFanOutByModule.Keys)
                modules.Add(NormalizeModuleName(m));

            if (architectural != null)
            {
                foreach (var item in architectural.Items)
                {
                    if (!string.IsNullOrWhiteSpace(item.Namespace))
                    {
                        modules.Add(ExtractModuleFromNamespace(item.Namespace));
                    }
                }
            }

            return modules
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .OrderBy(x => x)
                .ToList();
        }

        private static List<ModuleRouteEdge> BuildEdges(
            CouplingResult coupling,
            IReadOnlyList<string> knownModules)
        {
            var edges = new Dictionary<(string From, string To), int>();

            foreach (var originModule in coupling.TypeFanOutByModule)
            {
                var fromModule = NormalizeModuleName(originModule.Key);

                foreach (var typeRef in originModule.Value)
                {
                    var targetModule = ResolveTargetModule(typeRef.Key, knownModules);

                    if (string.IsNullOrWhiteSpace(targetModule))
                        continue;

                    if (string.Equals(fromModule, targetModule, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var key = (fromModule, targetModule);

                    if (!edges.ContainsKey(key))
                        edges[key] = 0;

                    edges[key] += Math.Max(1, typeRef.Value);
                }
            }

            return edges
                .Select(kvp => new ModuleRouteEdge
                {
                    From = kvp.Key.From,
                    To = kvp.Key.To,
                    Type = "uni",
                    Weight = kvp.Value,
                    Traffic = NormalizeTraffic(kvp.Value)
                })
                .OrderByDescending(e => e.Weight)
                .ToList();
        }

        private static List<ModuleRouteEdge> MergeBidirectionalEdges(List<ModuleRouteEdge> rawEdges)
        {
            var consumed = new HashSet<int>();
            var merged = new List<ModuleRouteEdge>();

            for (int i = 0; i < rawEdges.Count; i++)
            {
                if (consumed.Contains(i))
                    continue;

                var current = rawEdges[i];
                var reverseIndex = rawEdges.FindIndex(e =>
                    !ReferenceEquals(e, current) &&
                    string.Equals(e.From, current.To, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(e.To, current.From, StringComparison.OrdinalIgnoreCase));

                if (reverseIndex >= 0 && !consumed.Contains(reverseIndex))
                {
                    var reverse = rawEdges[reverseIndex];
                    var combinedWeight = Math.Max(current.Weight, reverse.Weight);

                    merged.Add(new ModuleRouteEdge
                    {
                        From = current.From,
                        To = current.To,
                        Type = "bi",
                        Weight = combinedWeight,
                        Traffic = NormalizeTraffic(combinedWeight)
                    });

                    consumed.Add(i);
                    consumed.Add(reverseIndex);
                }
                else
                {
                    merged.Add(current);
                    consumed.Add(i);
                }
            }

            return merged;
        }

        private static List<ModuleRouteNode> BuildNodes(
            IReadOnlyList<string> knownModules,
            IReadOnlyList<ModuleRouteEdge> edges,
            EntryPointHeuristicResult? entryPoints,
            ImplicitCouplingResult? implicitCoupling,
            SolidResult? solid)
        {
            var entryModules = ResolveEntryModules(entryPoints, knownModules);
            var hubModules = ResolveHubModules(implicitCoupling, knownModules);
            var solidPressureByModule = ResolveSolidPressure(solid, knownModules);
            var implicitPressureByModule = ResolveImplicitPressure(implicitCoupling, knownModules);

            var nodes = new List<ModuleRouteNode>();

            foreach (var module in knownModules)
            {
                var kind = InferNodeKind(module, entryModules, hubModules);
                var subtitle = GetSubtitle(kind);

                double pressure = InferBaselinePressure(module, kind);

                if (implicitPressureByModule.TryGetValue(module, out var implicitPressure))
                    pressure = Math.Max(pressure, implicitPressure);

                if (solidPressureByModule.TryGetValue(module, out var solidPressure))
                    pressure = Math.Max(pressure, solidPressure);

                nodes.Add(new ModuleRouteNode
                {
                    Id = ToId(module),
                    Label = module,
                    Kind = kind,
                    Subtitle = subtitle,
                    Pressure = Math.Round(Math.Clamp(pressure, 0.15, 1.0), 2),
                    IsEntry = kind == "entry" || kind == "bootstrap",
                    IsHub = kind == "hub",
                    IsExit = kind == "exit"
                });
            }

            EnsureAtLeastOneHub(nodes, edges);
            EnsureAtLeastOneExit(nodes, edges);

            return nodes;
        }

        private static void ComputeNodeMetrics(
            IReadOnlyList<ModuleRouteNode> nodes,
            IReadOnlyList<ModuleRouteEdge> edges)
        {
            var nodeById = nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var edge in edges)
            {
                if (!nodeById.TryGetValue(ToId(edge.From), out var from))
                    continue;

                if (!nodeById.TryGetValue(ToId(edge.To), out var to))
                    continue;

                if (edge.Type == "bi")
                {
                    from.OutDegree += 1;
                    from.InDegree += 1;
                    to.OutDegree += 1;
                    to.InDegree += 1;

                    from.WeightedOut += edge.Weight;
                    from.WeightedIn += edge.Weight;
                    to.WeightedOut += edge.Weight;
                    to.WeightedIn += edge.Weight;
                }
                else
                {
                    from.OutDegree += 1;
                    to.InDegree += 1;

                    from.WeightedOut += edge.Weight;
                    to.WeightedIn += edge.Weight;
                }
            }

            var maxTraffic = nodes.Any() ? Math.Max(1.0, nodes.Max(n => n.Traffic)) : 1.0;

            foreach (var node in nodes)
            {
                node.HubScore = Math.Round(node.Traffic / maxTraffic, 2);
            }
        }

        private static HashSet<string> ResolveEntryModules(
            EntryPointHeuristicResult? entryPoints,
            IReadOnlyList<string> knownModules)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (entryPoints != null)
            {
                foreach (var entry in entryPoints.EntryPoints)
                {
                    var module = ResolveTargetModule(entry, knownModules);
                    if (!string.IsNullOrWhiteSpace(module))
                        result.Add(module);
                }
            }

            foreach (var module in knownModules)
            {
                if (LooksLikeEntryModule(module))
                    result.Add(module);
            }

            return result;
        }

        private static HashSet<string> ResolveHubModules(
            ImplicitCouplingResult? implicitCoupling,
            IReadOnlyList<string> knownModules)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (implicitCoupling == null)
                return result;

            foreach (var suspicion in implicitCoupling.Suspicions)
            {
                var sourceModule = ResolveTargetModule(suspicion.Module, knownModules);
                if (!string.IsNullOrWhiteSpace(sourceModule))
                    result.Add(sourceModule);

                var targetModule = ResolveTargetModule(suspicion.TargetModule, knownModules);
                if (!string.IsNullOrWhiteSpace(targetModule) && suspicion.Dominance >= 0.60)
                    result.Add(targetModule);
            }

            return result;
        }

        private static Dictionary<string, double> ResolveImplicitPressure(
    ImplicitCouplingResult? implicitCoupling,
    IReadOnlyList<string> knownModules)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (implicitCoupling == null)
                return result;

            var grouped = implicitCoupling.Suspicions
                .GroupBy(s => ResolveTargetModule(s.Module, knownModules))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(
                    g => g.Key!,
                    g => g.Max(x => Math.Max(
                        x.Dominance,
                        Math.Clamp(x.Volume / 10.0, 0.15, 1.0))),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var kv in grouped)
                result[kv.Key] = Math.Round(Math.Clamp(kv.Value, 0.15, 1.0), 2);

            return result;
        }

        private static Dictionary<string, double> ResolveSolidPressure(
            SolidResult? solid,
            IReadOnlyList<string> knownModules)
        {
            var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (solid == null)
                return result;

            var grouped = solid.Alerts
                .GroupBy(a => ResolveTargetModule(a.Namespace, knownModules))
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .ToDictionary(g => g.Key!, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var max = grouped.Count == 0 ? 1 : grouped.Max(x => x.Value);

            foreach (var item in grouped)
            {
                result[item.Key] = Math.Round(Math.Clamp(item.Value / (double)max, 0.15, 1.0), 2);
            }

            return result;
        }

        private static string InferNodeKind(
           string module,
           HashSet<string> entryModules,
           HashSet<string> hubModules)
        {
            if (LooksLikeBootstrapModule(module))
                return "bootstrap";

            if (entryModules.Contains(module) || LooksLikeEntryModule(module))
                return "entry";

            if (LooksLikeStrongHubModule(module))
                return "hub";

            if (hubModules.Contains(module) || LooksLikeHubModule(module))
                return "hub";

            if (LooksLikeExporterModule(module) || LooksLikeExitModule(module))
                return "exit";

            if (LooksLikeStationModule(module))
                return "station";

            if (LooksLikeSupportModule(module))
                return "support";

            return "process";
        }

        private static void EnsureAtLeastOneHub(
     List<ModuleRouteNode> nodes,
     IReadOnlyList<ModuleRouteEdge> edges)
        {
            if (nodes.Any(n => n.Kind == "hub"))
                return;

            var traffic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
                traffic[node.Label] = 0;

            foreach (var edge in edges)
            {
                if (!string.IsNullOrWhiteSpace(edge.From) && traffic.ContainsKey(edge.From))
                    traffic[edge.From] += edge.Weight;

                if (!string.IsNullOrWhiteSpace(edge.To) && traffic.ContainsKey(edge.To))
                    traffic[edge.To] += edge.Weight;
            }

            var candidate = nodes
                .Where(n => n.Kind != "bootstrap" && n.Kind != "support" && n.Kind != "exit")
                .OrderByDescending(n => traffic.GetValueOrDefault(n.Label))
                .Select(n => n.Label)
                .FirstOrDefault(label => traffic.ContainsKey(label) && traffic[label] > 0);

            if (candidate == null)
                return;

            var targetNode = nodes.First(x =>
                string.Equals(x.Label, candidate, StringComparison.OrdinalIgnoreCase));

            ReplaceNodeKind(nodes, targetNode, "hub");
        }

        private static void EnsureAtLeastOneExit(
            List<ModuleRouteNode> nodes,
            IReadOnlyList<ModuleRouteEdge> edges)
        {
            if (nodes.Any(n => n.Kind == "exit"))
                return;

            var incoming = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var outgoing = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                incoming[node.Label] = 0;
                outgoing[node.Label] = 0;
            }

            foreach (var edge in edges)
            {
                if (!string.IsNullOrWhiteSpace(edge.From) && outgoing.ContainsKey(edge.From))
                    outgoing[edge.From] += edge.Weight;

                if (!string.IsNullOrWhiteSpace(edge.To) && incoming.ContainsKey(edge.To))
                    incoming[edge.To] += edge.Weight;

                if (edge.Type == "bi")
                {
                    if (!string.IsNullOrWhiteSpace(edge.To) && outgoing.ContainsKey(edge.To))
                        outgoing[edge.To] += edge.Weight;

                    if (!string.IsNullOrWhiteSpace(edge.From) && incoming.ContainsKey(edge.From))
                        incoming[edge.From] += edge.Weight;
                }
            }

            var candidate = nodes
            .Where(n => n.Kind != "entry" && n.Kind != "bootstrap" && n.Kind != "hub")
                .OrderByDescending(n => incoming[n.Label] - outgoing[n.Label])
                .Select(n => n.Label)
                .FirstOrDefault();

            if (candidate == null)
                return;

            var targetNode = nodes.First(x => string.Equals(x.Label, candidate, StringComparison.OrdinalIgnoreCase));
            ReplaceNodeKind(nodes, targetNode, "exit");
        }

        private static void ReplaceNodeKind(List<ModuleRouteNode> nodes, ModuleRouteNode source, string newKind)
        {
            var index = nodes.IndexOf(source);
            if (index < 0)
                return;

            nodes[index] = new ModuleRouteNode
            {
                Id = source.Id,
                Label = source.Label,
                Kind = newKind,
                Subtitle = GetSubtitle(newKind),
                Pressure = source.Pressure,
                InDegree = source.InDegree,
                OutDegree = source.OutDegree,
                WeightedIn = source.WeightedIn,
                WeightedOut = source.WeightedOut,
                HubScore = source.HubScore,
                IsEntry = newKind == "entry",
                IsHub = newKind == "hub",
                IsExit = newKind == "exit"
            };
        }

        private static string ResolveTargetModule(string raw, IReadOnlyList<string> knownModules)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var normalized = raw.Trim();

            // 1. Match exato com módulo conhecido
            var direct = knownModules.FirstOrDefault(m =>
                normalized.Equals(m, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            // 2. Match por namespace/prefixo
            direct = knownModules.FirstOrDefault(m =>
                normalized.StartsWith(m + ".", StringComparison.OrdinalIgnoreCase) ||
                normalized.Contains("." + m + ".", StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith("." + m, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            // 3. Extrai módulo do namespace e valida
            var extracted = ExtractModuleFromNamespace(normalized);

            if (!string.IsNullOrWhiteSpace(extracted))
            {
                var validated = knownModules.FirstOrDefault(m =>
                    string.Equals(m, extracted, StringComparison.OrdinalIgnoreCase) ||
                    m.Contains(extracted, StringComparison.OrdinalIgnoreCase) ||
                    extracted.Contains(m, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(validated))
                    return validated;
            }

            return string.Empty;
        }

        private static string ExtractModuleFromNamespace(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var parts = raw.Split('.', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return string.Empty;

            if (parts.Length >= 2 && parts[0].Equals("RefactorScope", StringComparison.OrdinalIgnoreCase))
                return parts[1];

            return parts[0];
        }

        private static string NormalizeModuleName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var trimmed = raw.Trim();
            return ExtractModuleFromNamespace(trimmed);
        }

        private static double InferBaselinePressure(string module, string kind)
        {
            if (kind == "hub") return 0.78;
            if (kind == "station") return 0.42;
            if (kind == "entry") return 0.26;
            if (kind == "exit") return 0.24;

            if (module.Contains("Core", StringComparison.OrdinalIgnoreCase))
                return 0.58;

            return 0.36;
        }

        private static double NormalizeTraffic(int weight)
        {
            return Math.Round(Math.Clamp(weight / 8.0, 0.2, 1.0), 2);
        }

        private static string ToId(string module)
        {
            return module
                .Replace(".", "_", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", "_", StringComparison.OrdinalIgnoreCase)
                .ToLowerInvariant();
        }

        //private static bool LooksLikeEntryModule(string module)
        //{
        //    return module.Contains("Cli", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Program", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Startup", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Input", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Config", StringComparison.OrdinalIgnoreCase);
        //}

        private static bool LooksLikeEntryModule(string module)
        {
            return ContainsAny(module,
                "Cli", "Input", "Config", "Command", "Host", "Loader",
                "Entrada", "Leitura", "Configuracao", "Configuração", "Comando", "Carregador");
        }

        //private static bool LooksLikeExitModule(string module)
        //{
        //    return module.Contains("Exporter", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Output", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Writer", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Renderer", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Dashboard", StringComparison.OrdinalIgnoreCase);
        //}

        private static bool LooksLikeExitModule(string module)
        {
            return LooksLikeExporterModule(module);
        }

        //private static bool LooksLikeStationModule(string module)
        //{
        //    return module.Contains("Result", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Dto", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Context", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Model", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Report", StringComparison.OrdinalIgnoreCase);
        //}

        private static bool LooksLikeStationModule(string module)
        {
            return ContainsAny(module,
                "Result", "Results", "Dto", "DTO", "Context", "Contexts", "Report", "Model",
                "Resultado", "Resultados", "Contexto", "Contextos", "Relatorio", "Relatório", "Modelo");
        }

        //private static bool LooksLikeHubModule(string module)
        //{
        //    return module.Contains("Core", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Orchestration", StringComparison.OrdinalIgnoreCase)
        //        || module.Contains("Hub", StringComparison.OrdinalIgnoreCase);
        //}

        private static bool LooksLikeHubModule(string module)
        {
            return ContainsAny(module,
                "Hub", "Coordinator", "Coordinator", "Kernel",
                "Coordenador", "Coordenacao", "Coordenação");
        }

        private static bool LooksLikeBootstrapModule(string module)
        {
            return ContainsAny(module,
                "Program", "Main", "Startup", "App", "Bootstrap",
                "Programa", "Inicializacao", "Inicialização", "EntradaPrincipal");
        }


        private static bool LooksLikeStrongHubModule(string module)
        {
            return ContainsAny(module,
                "Nucleo", "Núcleo", "Core", "Orquestr", "Orchestr");
        }


        private static bool LooksLikeSupportModule(string module)
        {
            return ContainsAny(module,
                "Infrastructure", "Infra", "Common", "Shared", "Utils", "Utility", "Base",
                "Infraestrutura", "Comum", "Compartilhado", "Util", "Utils", "Utilitario", "Utilitário", "Base");
        }

        private static bool LooksLikeExporterModule(string module)
        {
            return ContainsAny(module,
                "Exporter", "Export", "Output", "Writer", "Serializer", "Report", "Dashboard",
                "Csv", "Json", "Txt", "Text", "Excel", "Xlsx", "Markdown", "Md",
                "Exportador", "Saida", "Saída", "Escritor", "Serializador", "Relatorio", "Relatório");
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

        private static string GetSubtitle(string kind)
        {
            return kind switch
            {
                "bootstrap" => "BOOTSTRAP",
                "entry" => "ENTRY_POINT",
                "station" => "TRANSIT",
                "hub" => "CONSOLIDATION",
                "support" => "SUPPORT_LAYER",
                "exit" => "EXPORT_OUTPUT",
                _ => "PROCESSING"
            };
        }


    }
}