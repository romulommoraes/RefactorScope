namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Define o tipo de evidência que originou uma referência entre tipos.
    /// 
    /// A qualificação da evidência permite análises mais sofisticadas
    /// sobre o grafo de dependência, como detecção de código morto
    /// ou classificação da intensidade do acoplamento.
    /// </summary>
    public enum TipoReferencia
    {
        /// <summary>
        /// Menção simples do tipo no código.
        /// Exemplo:
        /// UserService service;
        /// </summary>
        Mention,

        /// <summary>
        /// Instanciação direta do tipo.
        /// Exemplo:
        /// new UserService();
        /// </summary>
        Instantiation,

        /// <summary>
        /// Chamada estática de método.
        /// Exemplo:
        /// UserService.DoSomething();
        /// </summary>
        StaticCall,

        /// <summary>
        /// Uso dentro de generics.
        /// Exemplo:
        /// List&lt;UserService&gt;
        /// </summary>
        Generic,

        /// <summary>
        /// Uso em typeof().
        /// Exemplo:
        /// typeof(UserService)
        /// </summary>
        Typeof,

        /// <summary>
        /// Uso em nameof().
        /// Exemplo:
        /// nameof(UserService)
        /// </summary>
        Nameof,

        /// <summary>
        /// Declaração de variável tipada.
        /// Exemplo:
        /// UserService service;
        /// </summary>
        Declaration
    }

    /// <summary>
    /// Representa uma referência estrutural entre dois tipos
    /// dentro do grafo de dependência do projeto.
    /// </summary>
    public class ReferenciaInfo
    {
        /// <summary>
        /// Tipo que origina a referência.
        /// </summary>
        public string FromType { get; }

        /// <summary>
        /// Tipo referenciado.
        /// </summary>
        public string ToType { get; }

        /// <summary>
        /// Tipo de evidência que originou a referência.
        /// </summary>
        public TipoReferencia Kind { get; }

        public ReferenciaInfo(
            string fromType,
            string toType,
            TipoReferencia kind = TipoReferencia.Mention)
        {
            FromType = fromType;
            ToType = toType;
            Kind = kind;
        }
    }
}