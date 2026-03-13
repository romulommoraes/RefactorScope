namespace RefactorScope.Parsers.Analysis;

/// <summary>
/// Resultado da análise de complexidade de classes.
/// </summary>
public class ClassComplexityScanResult
{
    public List<string> SafeClasses { get; } = new();

    public List<string> ComplexClasses { get; } = new();
}