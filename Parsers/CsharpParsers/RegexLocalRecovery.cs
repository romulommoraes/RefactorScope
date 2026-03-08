using RefactorScope.Core.Model;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RefactorScope.Parsers.CsharpParsers
{
    /// <summary>
    /// Estratégia de recuperação local baseada em Regex.
    ///
    /// Utilizada quando o TextualParser perde o escopo da classe
    /// (geralmente por desbalanceamento de chaves causado por:
    /// interpolated strings, lambdas, inicializadores complexos, etc).
    ///
    /// IMPORTANTE
    /// ----------
    /// • Atua apenas no trecho da classe atual
    /// • Não reanalisa o arquivo inteiro
    /// • Não substitui o parser textual
    /// • Apenas tenta recuperar referências que podem ter sido perdidas
    ///
    /// Complexidade aproximada:
    ///
    /// O(n × t)
    ///
    /// onde:
    /// n = tamanho do trecho analisado
    /// t = número de tipos conhecidos no modelo
    ///
    /// Como o trecho normalmente é apenas a classe atual,
    /// o impacto de performance é mínimo.
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

                if (Regex.IsMatch(classSource, $@"\b{Regex.Escape(target)}\b"))
                {
                    refs.Add(new ReferenciaInfo(
                        fromType,
                        target,
                        TipoReferencia.Mention));
                }
            }

            return refs;
        }
    }
}