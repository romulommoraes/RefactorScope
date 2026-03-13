using System.IO;
using RefactorScope.Core.Abstractions;

namespace RefactorScope.Core.Parsing
{
    /// <summary>
    /// Provedor de código fonte sanitizado.
    ///
    /// Centraliza a leitura de arquivos e a aplicação
    /// de um PreParser antes do parsing estrutural.
    /// </summary>
    public sealed class SanitizedSourceProvider
    {
        private readonly IPreParser preParser;

        public SanitizedSourceProvider(IPreParser preParser)
        {
            this.preParser = preParser;
        }

        public string Read(string path)
        {
            var source = File.ReadAllText(path);
            return preParser.Sanitize(source);
        }
    }
}