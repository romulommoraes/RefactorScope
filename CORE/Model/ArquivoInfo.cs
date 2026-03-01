using System.Reflection;

namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Representa um arquivo dentro do escopo analisado.
    /// </summary>
    public class ArquivoInfo
    {
        /// <summary>
        /// Caminho relativo ao root analisado.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Namespace detectado no arquivo.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Conteúdo do código fonte.
        /// </summary>
        public string SourceCode { get; }

        /// <summary>
        /// Tipos declarados neste arquivo.
        /// </summary>
        public IReadOnlyList<TipoInfo> Tipos { get; }

        public ArquivoInfo(
            string relativePath,
            string @namespace,
            string sourceCode,
            IReadOnlyList<TipoInfo> tipos)
        {
            RelativePath = relativePath;
            Namespace = @namespace;
            SourceCode = sourceCode;
            Tipos = tipos;
        }
    }
}