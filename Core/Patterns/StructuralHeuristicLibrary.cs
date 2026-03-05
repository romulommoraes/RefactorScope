using RefactorScope.Core.Model;

namespace RefactorScope.Core.Patterns
{
    /// <summary>
    /// Heurísticas estruturais leves baseadas apenas
    /// no modelo estrutural disponível (TipoInfo).
    ///
    /// Não depende de AST nem reflexão.
    /// Usa:
    /// - nome da classe
    /// - namespace
    /// - tipo (class/interface/etc)
    /// - referências detectadas
    /// </summary>
    public static class StructuralHeuristicLibrary
    {
        public static PatternSignatureResult Evaluate(TipoInfo tipo)
        {
            // ==============================
            // CLI / Infrastructure helpers
            // ==============================

            if (tipo.Namespace.Contains(".CLI"))
                return PatternSignatureResult.Match("CliInfrastructure");

            if (tipo.Namespace.Contains(".Infrastructure"))
                return PatternSignatureResult.Match("InfrastructureUtility");

            if (tipo.Namespace.Contains(".Configuration"))
                return PatternSignatureResult.Match("ConfigurationComponent");


            // ==============================
            // Orchestrators
            // ==============================

            if (tipo.Name.Contains("Orchestrator"))
                return PatternSignatureResult.Match("Orchestrator");


            // ==============================
            // Validators
            // ==============================

            if (tipo.Name.Contains("Validator"))
                return PatternSignatureResult.Match("Validator");


            // ==============================
            // Loaders
            // ==============================

            if (tipo.Name.Contains("Loader"))
                return PatternSignatureResult.Match("Loader");


            // ==============================
            // Renderers
            // ==============================

            if (tipo.Name.Contains("Renderer"))
                return PatternSignatureResult.Match("Renderer");


            // ==============================
            // Consolidators
            // ==============================

            if (tipo.Name.Contains("Consolidator"))
                return PatternSignatureResult.Match("Aggregator");


            // ==============================
            // Discovery / scanning utilities
            // ==============================

            if (tipo.Name.Contains("Discovery"))
                return PatternSignatureResult.Match("DiscoveryService");


            // ==============================
            // Logging utilities
            // ==============================

            if (tipo.Name.Contains("Logger"))
                return PatternSignatureResult.Match("LoggingUtility");


            // ==============================
            // Extension classes
            // ==============================

            if (tipo.Name.EndsWith("Extensions"))
                return PatternSignatureResult.Match("Extension");


            // ==============================
            // Configuration objects
            // ==============================

            if (tipo.Name.EndsWith("Options")
                || tipo.Name.EndsWith("Configuration"))
            {
                return PatternSignatureResult.Match("ConfigurationObject");
            }


            // ==============================
            // CQRS / Messaging
            // ==============================

            if (tipo.Name.EndsWith("Command")
                || tipo.Name.EndsWith("Query")
                || tipo.Name.EndsWith("Event")
                || tipo.Name.EndsWith("Request")
                || tipo.Name.EndsWith("Response"))
            {
                return PatternSignatureResult.Match("MessagingType");
            }


            // ==============================
            // DTO / mapping
            // ==============================

            if (tipo.Name.EndsWith("Dto")
                || tipo.Name.EndsWith("Mapper")
                || tipo.Name.EndsWith("Converter"))
            {
                return PatternSignatureResult.Match("MappingType");
            }


            return PatternSignatureResult.None();
        }
    }
}