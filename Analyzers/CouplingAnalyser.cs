using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// CouplingAnalyzer
    ///
    /// Calcula métricas estruturais de acoplamento inspiradas no modelo
    /// clássico de métricas arquiteturais de Robert C. Martin (NDepend).
    ///
    /// Métricas calculadas:
    ///
    /// -------------------------------------------------
    /// Nível de Classe
    /// -------------------------------------------------
    ///
    /// FanOutTotal (Ce)
    /// Número total de dependências que saem da classe.
    ///
    /// FanIn (Ca)
    /// Número de dependências que chegam na classe.
    ///
    /// Instability
    /// I = Ce / (Ce + Ca)
    ///
    /// 0.0 → classe estável
    /// 1.0 → classe instável
    ///
    /// -------------------------------------------------
    /// Nível Arquitetural (Módulo)
    /// -------------------------------------------------
    ///
    /// FanOutInterModule
    /// Dependências que cruzam limites de módulo.
    ///
    /// Abstractness (A)
    /// A = Na / Nc
    /// proporção de classes abstratas ou interfaces.
    ///
    /// Instability (I)
    /// I = Ce / (Ce + Ca)
    ///
    /// Distance from Main Sequence (D)
    /// D = | A + I - 1 |
    ///
    /// 0 → arquitetura equilibrada
    /// 1 → arquitetura problemática
    ///
    /// -------------------------------------------------
    /// Uso no RefactorScope
    /// -------------------------------------------------
    ///
    /// FanOutInterModule → Architectural Health
    /// FanOutTotal → heurística SRP (SOLID)
    /// Instability → métrica arquitetural
    /// Abstractness / Distance → estabilidade de módulos
    ///
    /// Inspirado em:
    /// Robert C. Martin — Clean Architecture
    /// </summary>
    public class CouplingAnalyzer : IAnalyzer
    {
        public string Name => "coupling";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var fanOutPorModulo = new Dictionary<string, int>();
            var fanOutPorTipoPorModulo = new Dictionary<string, Dictionary<string, int>>();

            var fanOutTotalPorTipo = new Dictionary<string, int>();
            var fanInPorTipo = new Dictionary<string, int>();
            var instabilityPorTipo = new Dictionary<string, double>();

            var tipos = context.Model.Tipos;
            var referencias = context.Model.Referencias;

            // -------------------------------------------------
            // mapa tipo → módulo
            // -------------------------------------------------

            var tipoParaModulo = tipos.ToDictionary(
                t => t.Name,
                t => ExtractTopFolder(t.DeclaredInFile)
            );

            // -------------------------------------------------
            // cálculo FanIn (Ca)
            // -------------------------------------------------

            foreach (var r in referencias)
            {
                if (!fanInPorTipo.ContainsKey(r.ToType))
                    fanInPorTipo[r.ToType] = 0;

                fanInPorTipo[r.ToType]++;
            }

            // -------------------------------------------------
            // cálculo principal por tipo
            // -------------------------------------------------

            foreach (var tipo in tipos)
            {
                var nome = tipo.Name;
                var moduloOrigem = tipoParaModulo[nome];

                var refsSaida = referencias
                    .Where(r => r.FromType == nome)
                    .ToList();

                int fanOutTotal = refsSaida.Count;
                int fanOutInterModule = 0;

                foreach (var r in refsSaida)
                {
                    if (!tipoParaModulo.TryGetValue(r.ToType, out var moduloDestino))
                        continue;

                    if (moduloDestino != moduloOrigem)
                        fanOutInterModule++;
                }

                fanOutTotalPorTipo[nome] = fanOutTotal;

                if (!fanOutPorModulo.ContainsKey(moduloOrigem))
                    fanOutPorModulo[moduloOrigem] = 0;

                fanOutPorModulo[moduloOrigem] += fanOutInterModule;

                if (!fanOutPorTipoPorModulo.ContainsKey(moduloOrigem))
                    fanOutPorTipoPorModulo[moduloOrigem] = new Dictionary<string, int>();

                fanOutPorTipoPorModulo[moduloOrigem][nome] = fanOutInterModule;

                var fanIn = fanInPorTipo.GetValueOrDefault(nome);

                double instability = 0;

                if (fanOutTotal + fanIn > 0)
                    instability = (double)fanOutTotal / (fanOutTotal + fanIn);

                instabilityPorTipo[nome] = instability;
            }

            // -------------------------------------------------
            // Métricas arquiteturais por módulo
            // -------------------------------------------------

            var abstractnessPorModulo = new Dictionary<string, double>();
            var instabilityPorModulo = new Dictionary<string, double>();
            var distancePorModulo = new Dictionary<string, double>();

            var tiposPorModulo = tipos.GroupBy(t => tipoParaModulo[t.Name]);

            foreach (var grupo in tiposPorModulo)
            {
                var modulo = grupo.Key;

                var totalTipos = grupo.Count();

                var abstratos = grupo.Count(t =>
                    t.IsInterface || t.IsAbstract);

                double abstractness = totalTipos == 0
                    ? 0
                    : (double)abstratos / totalTipos;

                int ce = fanOutPorModulo.GetValueOrDefault(modulo);

                int ca = referencias.Count(r =>
                    tipoParaModulo.TryGetValue(r.ToType, out var dest)
                    && dest == modulo);

                double instability = (ce + ca) == 0
                    ? 0
                    : (double)ce / (ce + ca);

                double distance = Math.Abs(abstractness + instability - 1);

                abstractnessPorModulo[modulo] = abstractness;
                instabilityPorModulo[modulo] = instability;
                distancePorModulo[modulo] = distance;
            }

            return new CouplingResult(
                fanOutPorModulo,
                fanOutPorTipoPorModulo,
                fanOutTotalPorTipo,
                fanInPorTipo,
                instabilityPorTipo,
                abstractnessPorModulo,
                instabilityPorModulo,
                distancePorModulo
            );
        }

        /// <summary>
        /// Extrai o módulo arquitetural baseado na pasta superior.
        ///
        /// Estrutura típica:
        ///
        /// Projeto/Modulo/arquivo.cs
        ///
        /// Exemplo:
        ///
        /// AvaliaRoteiro/Nucleo/OrquestradorNucleo.cs
        /// → módulo = Nucleo
        /// </summary>
        private static string ExtractTopFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Root";

            var parts = path
                .Replace("\\", "/")
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
                return parts[1];

            return parts[0];
        }
    }
}