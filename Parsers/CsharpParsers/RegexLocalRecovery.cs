using RefactorScope.Core.Model;
using RefactorScope.Parsers.Common;
using System.Text.RegularExpressions;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Estratégia de recuperação local baseada em Regex.
    ///
    /// Utilizada quando o TextualParser perde o escopo da classe
    /// por desbalanceamento de chaves ou ruído estrutural.
    ///
    /// Princípios:
    /// - atua apenas no trecho da classe atual
    /// - não reanalisa o arquivo inteiro
    /// - não substitui o parser textual
    /// - tenta recuperar apenas referências plausíveis
    ///
    /// Segurança:
    /// - só aceita alvos presentes no conjunto de tipos conhecidos
    /// - rejeita falsos positivos léxicos via StructuralTokenGuard
    /// </summary>
    internal static class RegexLocalRecovery
    {
        public static IEnumerable<ReferenciaInfo> Recover(
            string classSource,
            string fromType,
            HashSet<string> knownTypes)
        {
            var refs = new List<ReferenciaInfo>();

            foreach (var target in knownTypes)
            {
                if (target == fromType)
                    continue;

                if (!StructuralTokenGuard.IsValidReferenceTarget(target, knownTypes))
                    continue;

                if (Regex.IsMatch(classSource, $@"\b{Regex.Escape(target)}\b"))
                {
                    refs.Add(new ReferenciaInfo(
                        fromType,
                        target,
                        TipoReferencia.Mention));
                }
            }

            return refs
                .GroupBy(r => $"{r.FromType}|{r.ToType}|{r.Kind}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }
    }
}