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
/// O cálculo também aplica atenuações para tipos especiais de módulo:
///
/// Composition Root
///     Normalmente possui alto fan-out (registro de dependências).
///     Penalidade de coupling é fortemente reduzida.
///
/// Infrastructure
///     Naturalmente depende de múltiplos módulos.
///     Penalidade de coupling é parcialmente reduzida.
///
/// A normalização de tipos evita explosões em módulos pequenos
/// (ex: Program.cs com apenas 1 classe).
/// </summary>
public static class ArchitecturalScoreCalculator
{
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

        // -------------------------------------------------
        // Normalização estrutural
        // -------------------------------------------------
        // Evita explosões de penalidade em módulos com poucas classes.
        // Exemplo clássico: Program.cs (1 tipo, alto fan-out).
        // -------------------------------------------------

        double normalizedTypes = Math.Max(totalTypes, 3);

        double zombieRate = unresolvedCount / normalizedTypes;
        double isolationRate = isolatedCount / normalizedTypes;
        double couplingRate = fanOut / normalizedTypes;
        double coreDensity = coreTypes / normalizedTypes;

        // -------------------------------------------------
        // Ajustes estruturais por tipo de módulo
        // -------------------------------------------------

        if (IsCompositionRoot(module))
        {
            // Composition roots registram dependências,
            // portanto fan-out alto é esperado.
            couplingRate *= 0.10;

            // Zombie detection também é menos relevante aqui
            zombieRate *= 0.50;
        }
        else if (IsInfrastructure(module))
        {
            // Infraestrutura depende naturalmente de outros módulos
            couplingRate *= 0.50;
        }

        // -------------------------------------------------
        // Penalidades base
        // -------------------------------------------------

        double couplingPenalty = couplingRate * 30;
        double zombiePenalty = zombieRate * 25;
        double isolationPenalty = isolationRate * 20;

        // -------------------------------------------------
        // Bônus estrutural
        // -------------------------------------------------

        double coreBonus = coreDensity * 15;

        // -------------------------------------------------
        // Score final
        // -------------------------------------------------

        var score =
            100
            - couplingPenalty
            - zombiePenalty
            - isolationPenalty
            + coreBonus;

        return Math.Max(0, Math.Min(100, score));
    }

    // -------------------------------------------------
    // Heurísticas de identificação de módulos
    // -------------------------------------------------

    /// <summary>
    /// Detecta Composition Roots.
    ///
    /// Exemplos:
    /// Program
    /// Program.cs
    /// RaizComposicao
    /// CompositionRoot
    /// Bootstrap
    /// Startup
    ///
    /// Esses módulos normalmente:
    /// - possuem alto fan-out
    /// - possuem baixo fan-in
    /// - registram dependências (DI)
    /// </summary>
    private static bool IsCompositionRoot(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        module = module.Trim();

        return module.Contains("Program", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Raiz", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Composicao", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Composition", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Bootstrap", StringComparison.OrdinalIgnoreCase)
            || module.Contains("Startup", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Detecta módulos de infraestrutura.
    ///
    /// Infraestrutura normalmente:
    /// - depende de múltiplos módulos
    /// - contém adaptadores, exportadores, IO
    /// - não representa domínio central
    ///
    /// Penalidade de coupling é reduzida.
    /// </summary>
    private static bool IsInfrastructure(string module)
    {
        if (string.IsNullOrWhiteSpace(module))
            return false;

        module = module.Trim();

        return module.Equals("Infrastructure", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Infra", StringComparison.OrdinalIgnoreCase);
    }
}