namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Representa uma referência entre tipos.
    /// </summary>
    public class ReferenciaInfo
    {
        /// <summary>
        /// Tipo de origem da referência.
        /// </summary>
        public string FromType { get; }

        /// <summary>
        /// Tipo referenciado.
        /// </summary>
        public string ToType { get; }

        public ReferenciaInfo(string fromType, string toType)
        {
            FromType = fromType;
            ToType = toType;
        }
    }
}