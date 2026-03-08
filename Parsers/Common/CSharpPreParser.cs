using RefactorScope.Core.Abstractions;
using System.Text;

namespace RefactorScope.Parsers.Common
{
    /// <summary>
    /// PreParser minimalista.
    ///
    /// Remove apenas blocos XML documentation:
    ///
    /// /// &lt;summary&gt;
    ///     ...
    /// /// &lt;/summary&gt;
    ///
    /// A estrutura do arquivo é preservada.
    /// Nenhuma linha é removida — apenas limpa.
    /// </summary>
    public class CSharpPreParser : IPreParser
    {
        public string Sanitize(string source)
        {
            var reader = new StringReader(source);
            var output = new StringBuilder();

            bool insideSummary = false;

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;

                var trimmed = line.Trim();

                if (trimmed.StartsWith("/// <summary"))
                {
                    insideSummary = true;
                    output.AppendLine("");
                    continue;
                }

                if (insideSummary)
                {
                    if (trimmed.StartsWith("/// </summary"))
                    {
                        insideSummary = false;
                    }

                    output.AppendLine("");
                    continue;
                }

                output.AppendLine(line);
            }

            return output.ToString();
        }
    }
}