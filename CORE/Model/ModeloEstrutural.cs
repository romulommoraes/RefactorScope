namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Representa o modelo estrutural agnóstico do código analisado.
    /// </summary>
    public class ModeloEstrutural
    {
        /// <summary>
        /// Caminho raiz analisado.
        /// </summary>
        public string RootPath { get; }

        /// <summary>
        /// Arquivos encontrados no escopo.
        /// </summary>
        public IReadOnlyList<ArquivoInfo> Arquivos { get; }

        /// <summary>
        /// Todos os tipos detectados.
        /// </summary>
        public IReadOnlyList<TipoInfo> Tipos { get; }

        /// <summary>
        /// Todas as referências detectadas.
        /// </summary>
        public IReadOnlyList<ReferenciaInfo> Referencias { get; }

        public ModeloEstrutural(
            string rootPath,
            IReadOnlyList<ArquivoInfo> arquivos,
            IReadOnlyList<TipoInfo> tipos,
            IReadOnlyList<ReferenciaInfo> referencias)
        {
            RootPath = rootPath;
            Arquivos = arquivos;
            Tipos = tipos;
            Referencias = referencias;
        }
    }
}