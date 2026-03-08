using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using RefactorScope.Core.Parsing;
using RefactorScope.Parsers.Analysis;

namespace RefactorScope.Parsers.CsharpParsers.Hybrid
{
    /// <summary>
    /// Parser híbrido seletivo com baseline global.
    ///
    /// Estratégia:
    ///
    /// 1) Executa o parser Regex sobre TODO o escopo selecionado
    ///    -> Regex torna-se a espinha dorsal estrutural do modelo
    ///
    /// 2) Executa o scanner de complexidade
    ///    -> classifica arquivos em SAFE e COMPLEX
    ///
    /// 3) Executa o parser Textual sobre TODO o escopo
    ///    -> mas só considera, na consolidação, os artefatos vindos de arquivos SAFE
    ///
    /// 4) Consolida o resultado:
    ///    - Regex define baseline global
    ///    - Textual apenas complementa arquivos SAFE
    ///    - Regex vence conflitos
    ///    - nunca duplica tipo ou referência
    ///
    /// Motivação desta arquitetura:
    /// - preservar o contexto global de referências do Regex
    /// - evitar a perda de referências cruzadas ao particionar o projeto
    /// - usar o Textual como refinador seletivo, não como backbone global
    /// </summary>
    public class HybridSelectiveParser : IParserCodigo
    {
        private readonly IParserCodigo regexParser;
        private readonly IParserCodigo textualParser;
        private readonly Action<string>? warn;

        public string Name => "HybridSelectiveParser";

        public HybridSelectiveParser(
            IParserCodigo regexParser,
            IParserCodigo textualParser,
            Action<string>? warn = null)
        {
            this.regexParser = regexParser;
            this.textualParser = textualParser;
            this.warn = warn;
        }

        public IParserResult Parse(
            string rootPath,
            IEnumerable<string>? include = null,
            IEnumerable<string>? exclude = null)
        {
            var startedAtUtc = DateTime.UtcNow;

            warn?.Invoke("[Selective] Executando baseline global via Regex...");

            var regexResult = regexParser.Parse(rootPath, include, exclude);

            warn?.Invoke("[Selective] Scanning project for class complexity...");

            var classifier = new ClassComplexityClassifier();
            var scan = classifier.Scan(rootPath, include, exclude);

            warn?.Invoke($"[Selective] Safe files: {scan.SafeClasses.Count}");
            warn?.Invoke($"[Selective] Complex files: {scan.ComplexClasses.Count}");

            warn?.Invoke("[Selective] Executando refinamento global via Textual...");

            var textualResult = textualParser.Parse(rootPath, include, exclude);

            // --------------------------------------------------
            // Cenário 1: Regex falhou, Textual sobreviveu
            // --------------------------------------------------
            if (regexResult.Model == null && textualResult.Model != null)
            {
                warn?.Invoke("[Selective] Baseline Regex indisponível. Retornando Textual como fallback.");

                return new ParserResult(
                    ParseStatus.FallbackTriggered,
                    textualResult.IsPlausible,
                    textualResult.Confidence,
                    $"{Name} -> {textualResult.ParserName}",
                    textualResult.Model,
                    true,
                    BuildCombinedStats(startedAtUtc, regexResult, textualResult),
                    regexResult.Error ?? textualResult.Error);
            }

            // --------------------------------------------------
            // Cenário 2: ambos falharam
            // --------------------------------------------------
            if (regexResult.Model == null && textualResult.Model == null)
            {
                return new ParserResult(
                    ParseStatus.Failed,
                    false,
                    0,
                    Name,
                    null,
                    false,
                    BuildCombinedStats(startedAtUtc, regexResult, textualResult),
                    regexResult.Error ?? textualResult.Error);
            }

            // --------------------------------------------------
            // Cenário 3: Regex disponível, Textual indisponível
            // --------------------------------------------------
            if (regexResult.Model != null && textualResult.Model == null)
            {
                warn?.Invoke("[Selective] Refinamento Textual indisponível. Retornando baseline Regex.");

                return new ParserResult(
                    regexResult.IsPlausible ? ParseStatus.Success : ParseStatus.PlausibilityWarning,
                    regexResult.IsPlausible,
                    regexResult.Confidence,
                    Name,
                    regexResult.Model,
                    false,
                    BuildCombinedStats(startedAtUtc, regexResult, textualResult),
                    regexResult.Error);
            }

            // A partir daqui, ambos os modelos existem
            var regexModel = regexResult.Model!;
            var textualModel = textualResult.Model!;

            var safeRelativePaths = scan.SafeClasses
                .Select(f => Normalize(Path.GetRelativePath(rootPath, f)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // --------------------------------------------------
            // Base canônica: Regex global
            // --------------------------------------------------
            var mergedFiles = regexModel.Arquivos.ToList();
            var mergedTypes = regexModel.Tipos.ToList();
            var mergedReferences = regexModel.Referencias.ToList();

            // Índices para deduplicação e complemento seguro
            var knownTypeKeys = mergedTypes
                .Select(t => BuildTypeKey(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var knownTypeNames = mergedTypes
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var knownReferenceKeys = mergedReferences
                .Select(BuildReferenceKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // --------------------------------------------------
            // Extrai apenas o subconjunto SAFE do resultado textual
            // --------------------------------------------------
            var safeTextualTypes = textualModel.Tipos
                .Where(t => safeRelativePaths.Contains(Normalize(t.DeclaredInFile)))
                .ToList();

            var safeTextualTypeNames = safeTextualTypes
                .Select(t => t.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var safeTextualReferences = textualModel.Referencias
                .Where(r =>
                    safeTextualTypeNames.Contains(r.FromType) &&
                    (knownTypeNames.Contains(r.ToType) || safeTextualTypeNames.Contains(r.ToType)))
                .ToList();

            // --------------------------------------------------
            // 1) Complementa TIPOS
            // Regex continua canônico:
            // - se já existe no Regex, mantém Regex
            // - se o Textual trouxe algo novo em SAFE, adiciona
            // --------------------------------------------------
            foreach (var textualType in safeTextualTypes)
            {
                var key = BuildTypeKey(textualType);

                if (knownTypeKeys.Contains(key))
                    continue;

                mergedTypes.Add(textualType);
                knownTypeKeys.Add(key);
                knownTypeNames.Add(textualType.Name);
            }

            // --------------------------------------------------
            // 2) Complementa REFERÊNCIAS
            // Regex é base.
            // Textual só adiciona o que:
            // - vier de SAFE
            // - não exista ainda
            // - aponte para tipo conhecido no modelo final
            // --------------------------------------------------
            foreach (var textualReference in safeTextualReferences)
            {
                if (!knownTypeNames.Contains(textualReference.FromType))
                    continue;

                if (!knownTypeNames.Contains(textualReference.ToType))
                    continue;

                var key = BuildReferenceKey(textualReference);

                if (knownReferenceKeys.Contains(key))
                    continue;

                mergedReferences.Add(textualReference);
                knownReferenceKeys.Add(key);
            }

            // --------------------------------------------------
            // 3) Reinjeta referências nos tipos consolidados
            // --------------------------------------------------
            foreach (var tipo in mergedTypes)
            {
                var refs = mergedReferences
                    .Where(r => r.FromType == tipo.Name)
                    .ToList();

                typeof(TipoInfo)
                    .GetField(
                        "<References>k__BackingField",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(tipo, refs);
            }

            // --------------------------------------------------
            // 4) Reinjeta tipos nos arquivos canônicos do Regex
            // O Regex já viu o projeto todo, então seus ArquivoInfo
            // permanecem como base. Apenas atualizamos a lista de tipos.
            // --------------------------------------------------
            foreach (var arquivo in mergedFiles)
            {
                var tiposDoArquivo = mergedTypes
                    .Where(t => Normalize(t.DeclaredInFile)
                        .Equals(Normalize(arquivo.RelativePath), StringComparison.OrdinalIgnoreCase))
                    .ToList();

                typeof(ArquivoInfo)
                    .GetField(
                        "<Tipos>k__BackingField",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic)
                    ?.SetValue(arquivo, tiposDoArquivo);
            }

            var mergedModel = new ModeloEstrutural(
                rootPath,
                mergedFiles,
                mergedTypes,
                mergedReferences);

            bool plausible = PlausibilityEvaluator.Evaluate(mergedModel);

            double confidence = Math.Max(
                regexResult.Confidence,
                textualResult.Confidence);

            warn?.Invoke("[Selective] Baseline + selective refinement completed.");

            return new ParserResult(
                plausible ? ParseStatus.Success : ParseStatus.PlausibilityWarning,
                plausible,
                confidence,
                Name,
                mergedModel,
                false,
                BuildCombinedStats(startedAtUtc, regexResult, textualResult),
                regexResult.Error ?? textualResult.Error);
        }

        private static ParserExecutionStats BuildCombinedStats(
            DateTime startedAtUtc,
            IParserResult? regexResult,
            IParserResult? textualResult)
        {
            var elapsed = DateTime.UtcNow - startedAtUtc;

            var memory =
                (regexResult?.Stats?.EstimatedMemoryBytes ?? 0) +
                (textualResult?.Stats?.EstimatedMemoryBytes ?? 0);

            var anomalyDetected =
                (regexResult?.Stats?.AnomalyDetected ?? false) ||
                (textualResult?.Stats?.AnomalyDetected ?? false);

            return new ParserExecutionStats(
                elapsed,
                memory,
                anomalyDetected);
        }

        private static string BuildTypeKey(TipoInfo tipo)
            => $"{tipo.Namespace}|{tipo.Name}|{Normalize(tipo.DeclaredInFile)}";

        private static string BuildReferenceKey(ReferenciaInfo referencia)
            => $"{referencia.FromType}|{referencia.ToType}|{referencia.Kind}";

        private static string Normalize(string path)
            => path.Replace('\\', '/').Trim();
    }
}