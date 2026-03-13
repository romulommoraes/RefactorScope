using System.Text.RegularExpressions;

namespace RefactorScope.Parsers.Common
{
    /// <summary>
    /// Camada central de saneamento de tokens estruturais.
    ///
    /// Objetivos:
    /// - impedir falsos positivos léxicos conhecidos (ex.: "misuse")
    /// - validar identificadores candidatos a tipo
    /// - reduzir ruído antes que o token entre no modelo estrutural
    ///
    /// Esta classe é deliberadamente conservadora.
    /// Em caso de dúvida, o token é rejeitado.
    /// </summary>
    internal static class StructuralTokenGuard
    {
        private static readonly HashSet<string> ReservedKeywords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "with", "init", "var", "new", "return",
                "public", "private", "protected", "internal",
                "static", "void", "string", "int", "bool",
                "byte", "short", "long", "float", "double",
                "decimal", "object", "dynamic", "namespace",
                "class", "interface", "record", "struct",
                "enum", "delegate", "base", "this", "null",
                "true", "false", "default", "using", "global"
            };

        /// <summary>
        /// Blacklist de falsos positivos observados recorrentemente.
        ///
        /// Mantida pequena de propósito para não introduzir falsos negativos
        /// desnecessários. Pode crescer de forma incremental conforme a calibração.
        /// </summary>
        private static readonly HashSet<string> KnownFalsePositives =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "misuse",
                "summary",
                "remarks",
                "example",
                "param",
                "returns",
                "inheritdoc",
                "cref"
            };

        private static readonly Regex IdentifierRegex =
            new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// Valida se um token é aceitável como nome declarado de tipo.
        ///
        /// Regras:
        /// - identificador C# básico válido
        /// - não pode ser palavra reservada
        /// - não pode estar na blacklist de falsos positivos
        /// - deve seguir convenção de tipo (PascalCase) ou iniciar com '_'
        /// </summary>
        public static bool IsValidDeclaredTypeName(string? token)
        {
            if (!IsBasicIdentifier(token))
                return false;

            if (ReservedKeywords.Contains(token!))
                return false;

            if (KnownFalsePositives.Contains(token!))
                return false;

            return char.IsUpper(token![0]) || token[0] == '_';
        }

        /// <summary>
        /// Valida se um token pode ser aceito como alvo de referência.
        ///
        /// Regras:
        /// - precisa existir no conjunto de tipos conhecidos
        /// - identificador básico válido
        /// - não pode estar na blacklist de falsos positivos
        /// </summary>
        public static bool IsValidReferenceTarget(string? token, HashSet<string> knownTypes)
        {
            if (!IsBasicIdentifier(token))
                return false;

            if (KnownFalsePositives.Contains(token!))
                return false;

            return knownTypes.Contains(token!);
        }

        /// <summary>
        /// Validação sintática mínima de identificador.
        /// </summary>
        public static bool IsBasicIdentifier(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return IdentifierRegex.IsMatch(token!);
        }
    }
}