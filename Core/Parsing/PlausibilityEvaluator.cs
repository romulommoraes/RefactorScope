using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using System.Linq;

namespace RefactorScope.Core.Parsing;

/// <summary>
/// Avaliador heurístico projetado para detectar falhas silenciosas em parsers 
/// (ex: retornar um modelo válido, porém vazio ou estatisticamente improvável).
/// </summary>
public static class PlausibilityEvaluator
{
    /// <summary>
    /// Avalia se o modelo estrutural gerado faz sentido considerando o volume de código processado.
    /// </summary>
    /// <param name="model">O modelo estrutural a ser avaliado.</param>
    /// <returns>True se o modelo é plausível; False se parece ser lixo silencioso.</returns>
    public static bool Evaluate(ModeloEstrutural model)
    {
        if (model == null || model.Arquivos == null || !model.Arquivos.Any())
            return false;

        int totalLines = 0;
        int totalTipos = model.Tipos?.Count ?? 0;

        foreach (var arquivo in model.Arquivos)
        {
            if (!string.IsNullOrEmpty(arquivo.SourceCode))
            {
                // Otimização: Evita alocações massivas na LOH (Large Object Heap) que ocorreriam com .Split('\n')
                totalLines += arquivo.SourceCode.Count(c => c == '\n') + 1;
            }
        }

        // Heurística de exemplo: Se tem mais de 500 linhas de código e 0 tipos extraídos, 
        // o parser (provavelmente regex) engoliu o código sem quebrar, mas falhou na extração.
        if (totalLines > 500 && totalTipos == 0)
        {
            return false;
        }

        return true;
    }
}