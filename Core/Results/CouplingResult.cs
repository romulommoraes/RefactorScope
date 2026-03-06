using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Results
{
    /// <summary>
    /// Resultado do cálculo de acoplamento estrutural.
    ///
    /// Contém métricas em dois níveis:
    ///
    /// Classe:
    /// - FanOutTotal
    /// - FanIn
    /// - Instability
    ///
    /// Arquitetura:
    /// - FanOutInterModule
    /// - Abstractness
    /// - Instability (module)
    /// - Distance from Main Sequence
    /// </summary>
    public class CouplingResult : IAnalysisResult
    {
        // -------------------------------------------------
        // Arquitetura
        // -------------------------------------------------

        public IReadOnlyDictionary<string, int> ModuleFanOut { get; }

        public IReadOnlyDictionary<string, Dictionary<string, int>> TypeFanOutByModule { get; }

        // -------------------------------------------------
        // Classe
        // -------------------------------------------------

        public IReadOnlyDictionary<string, int> FanOutTotalByType { get; }

        public IReadOnlyDictionary<string, int> FanInByType { get; }

        public IReadOnlyDictionary<string, double> InstabilityByType { get; }

        // -------------------------------------------------
        // Arquitetura avançada
        // -------------------------------------------------

        public IReadOnlyDictionary<string, double> AbstractnessByModule { get; }

        public IReadOnlyDictionary<string, double> InstabilityByModule { get; }

        public IReadOnlyDictionary<string, double> DistanceByModule { get; }

        public CouplingResult(
            Dictionary<string, int> moduleFanOut,
            Dictionary<string, Dictionary<string, int>> typeFanOutByModule,
            Dictionary<string, int> fanOutTotalByType,
            Dictionary<string, int> fanInByType,
            Dictionary<string, double> instabilityByType,
            Dictionary<string, double> abstractnessByModule,
            Dictionary<string, double> instabilityByModule,
            Dictionary<string, double> distanceByModule)
        {
            ModuleFanOut = moduleFanOut;

            TypeFanOutByModule = typeFanOutByModule;

            FanOutTotalByType = fanOutTotalByType;

            FanInByType = fanInByType;

            InstabilityByType = instabilityByType;

            AbstractnessByModule = abstractnessByModule;

            InstabilityByModule = instabilityByModule;

            DistanceByModule = distanceByModule;
        }
    }
}