using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Configuration;
using RefactorScope.Core.Model;

namespace RefactorScope.Core.Context
{
    /// <summary>
    /// Representa o contexto de execução compartilhado entre os analisadores.
    /// Contém o modelo estrutural e a configuração ativa.
    /// </summary>
    public class AnalysisContext
    {
        /// <summary>
        /// Configuração da execução.
        /// </summary>
        public RefactorScopeConfig Config { get; }

        /// <summary>
        /// Modelo estrutural gerado pelo parser.
        /// </summary>
        public ModeloEstrutural Model { get; }

        /// <summary>
        /// Timestamp da execução.
        /// </summary>
        public DateTime ExecutionTime { get; }

        public AnalysisContext(
            RefactorScopeConfig config,
            ModeloEstrutural model)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Model = model ?? throw new ArgumentNullException(nameof(model));
            ExecutionTime = DateTime.UtcNow;
        }

        public IReadOnlyList<IAnalysisResult> Results { get; set; } = new List<IAnalysisResult>();
    }
}