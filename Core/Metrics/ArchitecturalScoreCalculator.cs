namespace RefactorScope.Core.Metrics;

/// <summary>
/// Calcula o score arquitetural de um módulo.
///
/// O score é um indicador heurístico (0–100) baseado em:
///
/// - Coupling (fan-out inter-módulo)
/// - Zombie candidates (classes potencialmente mortas)
/// - Isolation (core não referenciado)
/// - Core density (bônus estrutural)
///
/// Importante:
/// - Todos os módulos são penalizados por zombies.
/// - Apenas módulos arquiteturalmente relevantes recebem penalização
///   completa de coupling.
///
/// Alguns módulos possuem características estruturais específicas:
///
/// Composition Root
///     Normalmente possui alto fan-out (registro de dependências).
///     Penalidade de coupling é fortemente reduzida.
///
/// Infrastructure / Tooling
///     Dependem naturalmente de múltiplos módulos.
///     Penalidade de coupling é reduzida ou ignorada.
///
/// Observação:
/// A identificação desses módulos é heurística (baseada no nome do
/// módulo/folder). Portanto, a análise de coupling nesses casos
/// deve ser interpretada com cautela.
/// </summary>
public static class ArchitecturalScoreCalculator
{
    private const double MAX_COUPLING_PENALTY = 50;

    public static double Calculate(
        string module,
        int totalTypes,
        int unresolvedCount,
        int isolatedCount,
        int fanOut,
        int coreTypes)
    {
        if (totalTypes == 0)
            return 100;

        double normalizedTypes = Math.Max(totalTypes, 3);

        double zombieRate = unresolvedCount / normalizedTypes;
        double isolationRate = isolatedCount / normalizedTypes;
        double couplingRate = fanOut / normalizedTypes;
        double coreDensity = coreTypes / normalizedTypes;

        if (IsCompositionRoot(module))
        {
            couplingRate *= 0.10;
            zombieRate *= 0.50;
        }
        else if (IsInfrastructure(module))
        {
            couplingRate *= 0.50;
        }
        else if (IsToolingModule(module))
        {
            couplingRate = 0;
        }

        double rawCouplingPenalty = couplingRate * 30;

        // 🔧 Correção principal
        double couplingPenalty = Math.Min(rawCouplingPenalty, MAX_COUPLING_PENALTY);

        double zombiePenalty = zombieRate * 25;
        double isolationPenalty = isolationRate * 20;

        double coreBonus = coreDensity * 15;

        var score =
            100
            - couplingPenalty
            - zombiePenalty
            - isolationPenalty
            + coreBonus;

        return Math.Clamp(score, 0, 100);
    }

    internal static bool IsCompositionRoot(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        return module.Contains("Program", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Bootstrap", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Startup", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Composition", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Raiz", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsInfrastructure(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        return module.Equals("Infrastructure", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Infra", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Infraestrutura", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsToolingModule(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        return module.Equals("CLI", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Execution", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Exporters", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Exporter", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Statistics", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Debug", StringComparison.OrdinalIgnoreCase);
    }
}