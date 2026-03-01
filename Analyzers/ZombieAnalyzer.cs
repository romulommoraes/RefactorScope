using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Context;
using RefactorScope.Core.Model;
using RefactorScope.Core.Results;

namespace RefactorScope.Analyzers
{
    /// <summary>
    /// Detecta tipos não referenciados por outros tipos no escopo.
    /// </summary>
    public class ZombieAnalyzer : IAnalyzer
    {
        public string Name => "zombie";

        public IAnalysisResult Analyze(AnalysisContext context)
        {
            var tipos = context.Model.Tipos.Select(t => t.Name).ToHashSet();
            var referencias = context.Model.Referencias;

            var referenced = new HashSet<string>(
                referencias
                    .Where(r => tipos.Contains(r.ToType))
                    .Select(r => r.ToType)
            );

            var zombies = tipos
                .Where(t => !referenced.Contains(t))
                .ToList();

            return new ZombieResult(zombies);
        }
    }
}