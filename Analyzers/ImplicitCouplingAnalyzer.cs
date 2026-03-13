using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers;

public class ImplicitCouplingAnalyzer : IAnalyzer
{
    public string Name => "implicitCoupling";

    public IAnalysisResult Analyze(AnalysisContext context)
    {
        var tipos = context.Model.Tipos;
        var refs = context.Model.Referencias;

        var suspects = new List<CouplingSuspicion>();

        // mapa tipo → módulo
        var typeToModule = tipos.ToDictionary(
            t => t.Name,
            t => ExtractModule(t.DeclaredInFile));

        foreach (var tipo in tipos)
        {
            var origemModulo = typeToModule[tipo.Name];

            // ignorar composition root e infra
            if (IsInfrastructure(origemModulo))
                continue;

            var outgoing = refs
                .Where(r => r.FromType == tipo.Name)
                .ToList();

            var fanOut = outgoing.Count;

            var incoming = refs
                .Where(r => r.ToType == tipo.Name)
                .Count();

            int volume = fanOut + incoming;

            if (volume < 5)
                continue;

            var dominance = fanOut / (double)volume;

            if (dominance < 0.75)
                continue;

            var targetModules = outgoing
                .Select(r => typeToModule.GetValueOrDefault(r.ToType, "Unknown"))
                .Where(m => m != origemModulo)
                .GroupBy(m => m)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            if (targetModules == null)
                continue;

            suspects.Add(new CouplingSuspicion(
                tipo.Name,
                origemModulo,
                targetModules.Key,
                fanOut,
                incoming,
                Math.Round(dominance, 2),
                fanOut
            ));
        }
        Console.WriteLine($"[ImplicitCoupling] Suspects found: {suspects.Count}");
        return new ImplicitCouplingResult(suspects);
    }

    private static bool IsInfrastructure(string module)
    {
        return module.Equals("Infrastructure", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Program", StringComparison.OrdinalIgnoreCase)
            || module.Equals("Root", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractModule(string path)
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