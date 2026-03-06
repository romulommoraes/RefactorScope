namespace RefactorScope.Core.Model
{
    /// <summary>
    /// Representa um tipo estrutural do código.
    /// </summary>
    public class TipoInfo
    {
        /// <summary>
        /// Nome do tipo.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Namespace do tipo.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Tipo de declaração (class, interface, record, struct).
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Caminho do arquivo onde foi declarado.
        /// </summary>
        public string DeclaredInFile { get; }

        /// <summary>
        /// Referências feitas por este tipo.
        /// </summary>
        public IReadOnlyList<ReferenciaInfo> References { get; }

        /// <summary>
        /// Caminho completo do arquivo onde o tipo está definido.
        /// Necessário para geração do Dump IA.
        /// </summary>
        /// 
          // -------------------------------------------------
        // Propriedades derivadas
        // -------------------------------------------------

        /// <summary>
        /// Indica se o tipo é uma interface.
        /// </summary>
        public bool IsInterface =>
            Kind.Equals("interface", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Indica se o tipo é abstrato.
        /// </summary>
        /// Observação:
        /// O parser atual não diferencia "abstract class",
        /// portanto apenas interfaces são tratadas como abstratas.
        /// </summary>
        public bool IsAbstract =>
            Kind.Equals("interface", StringComparison.OrdinalIgnoreCase);


        public string FilePath { get; set; } = string.Empty;

        public TipoInfo(
            string name,
            string @namespace,
            string kind,
            string declaredInFile,
            IReadOnlyList<ReferenciaInfo> references)
        {
            Name = name;
            Namespace = @namespace;
            Kind = kind;
            DeclaredInFile = declaredInFile;
            References = references;
        }
    }
}