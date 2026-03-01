using RefactorScope.CORE.Context;

namespace RefactorScope.Core.Configuration
{
    /// <summary>
    /// Representa a configuração de execução do RefactorScope.
    /// Define escopo de análise, analisadores ativos e exportações.
    /// </summary>
    public class RefactorScopeConfig
    {
        /// <summary>
        /// Caminho raiz do escopo de análise.
        /// </summary>
        public string RootPath { get; set; } = string.Empty;

        /// <summary>
        /// Pastas ou arquivos a incluir na análise.
        /// </summary>
        public List<string> Include { get; set; } = new();

        /// <summary>
        /// Pastas ou arquivos a excluir da análise.
        /// </summary>
        public List<string> Exclude { get; set; } = new();

        /// <summary>
        /// Define quais analisadores estão habilitados.
        /// A chave deve corresponder ao Name do analisador.
        /// </summary>
        public Dictionary<string, bool> Analyzers { get; set; } = new();

        /// <summary>
        /// Nome do parser a ser utilizado.
        /// </summary>
        public string Parser { get; set; } = "CSharpRegex";

        /// <summary>
        /// Exportadores ativos.
        /// </summary>
        public List<string> Exporters { get; set; } = new();

        /// <summary>
        /// Camadas / Modulos do sistema e suas regras de dependência.
        /// </summary>

        public Dictionary<string, LayerRuleConfig>? LayerRules { get; set; }

        public string OutputPath { get; set; } = "refactorscope-output";
    }
}