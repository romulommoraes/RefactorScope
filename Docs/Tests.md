# Documentação da Suíte de Testes do RefactorScope

## Objetivo

Este documento descreve a cobertura atual de testes automatizados do RefactorScope, com foco nos módulos de Parsing, Arena e Structural Analysis. O objetivo da suíte é validar o comportamento funcional, reduzir regressões e registar explicitamente o contrato esperado dos componentes mais sensíveis do MVP.

## Visão Geral



A suíte atual cobre cinco áreas principais:

1.  **Parsers C#**
    * `CSharpRegexParser`
    * `CSharpTextualParser`
    * `HybridSelectiveParser`
2.  **Proteção Léxica**
    * `StructuralTokenGuard`
3.  **Arena**
    * `ParserArenaProjectResult`
    * `ParserArenaRunResult`
    * `ParserArenaScoreCalculator`
    * `ParserArenaOrchestrator`
4.  **Análise Estrutural**
    * `StructuralCandidateAnalyzer`
5.  **Refinamento Probabilístico Estrutural**
    * `StructuralCandidateRefinementAnalyzer`

---

## 1. Testes do CSharpRegexParser

**Ficheiro:** `RefactorScope.Tests/Parsing/CSharpRegexParserTests.cs`

### Coberturas Realizadas

* **`Parse_projeto_simples_retorna_modelo_valido`**
    Valida o fluxo básico do parser Regex num projeto mínimo contendo uma classe `Program`.
    *Verifica:*
    * Parser retorna resultado não nulo.
    * ParserName esperado: `CSharpRegex`.
    * Status diferente de `Failed`.
    * Presença de `Model` e `Stats`.
    * Detecção correta de: 1 ficheiro, 1 tipo, 0 referências.
    * Consistência entre `TipoInfo` e `ArquivoInfo`.

* **`Parse_detecta_multiplos_tipos_no_mesmo_projeto`**
    Valida detecção de múltiplos tipos distribuídos em ficheiros distintos.
    *Verifica:* Contagem correta de ficheiros/tipos e presença dos tipos `Program` e `PrimeService`.

* **`Parse_detecta_referencia_entre_tipos`**
    Valida detecção de referência simples por uso de tipo em código.
    *Verifica:* Existência de referências no modelo e presença de uma referência `Program -> PrimeService`.

* **`Parse_nao_detecta_false_positives_lexicos_como_tipos`**
    Protege contra regressões onde tokens textuais de comentários, XML docs ou string literals sejam tratados como tipos.
    *Verifica ausência de falsos positivos como:* `misuse`, `summary`, `remarks`, `param`, `returns`, `cref`.

* **`Parse_usa_namespace_global_quando_nao_existe_namespace_declarado`**
    Valida comportamento para ficheiros sem namespace explícito.
    *Verifica:* Tipo detectado com namespace `Global` e ficheiro também classificado em `Global`.

**Valor destes testes:**
Estes testes estabelecem o contrato mínimo do parser Regex e protegem especialmente contra falhas na extração básica de tipos, ruído léxico e quebra de namespace fallback.

---

## 2. Testes do CSharpTextualParser

**Ficheiro:** `RefactorScope.Tests/Parsing/CSharpTextualParserTests.cs`

### Coberturas Realizadas

* **`Parse_projeto_simples_retorna_modelo_valido`**
    Valida o fluxo básico do parser textual.
    *Verifica:* Resultado válido, ParserName esperado (`CSharpTextual`), presença de modelo/estatísticas e detecção correta de estruturas.

* **`Parse_detecta_multiplos_tipos_no_mesmo_projeto`**
    Valida múltiplos tipos em projeto simples.

* **`Parse_detecta_referencia_por_instanciacao_e_mencao`**
    Valida que o parser textual consegue diferenciar categorias de referência.
    *Verifica:* Referência `Instantiation` e referência `Mention` (ambas entre `Program` e `PrimeService`).

* **`Parse_detecta_referencias_por_typeof_nameof_e_generic`**
    Valida reconhecimento de padrões semânticos textuais adicionais.
    *Verifica:* `typeof(PrimeService)` → `TipoReferencia.Typeof`, `nameof(PrimeService)` → `TipoReferencia.Nameof`, `List<PrimeService>` → `TipoReferencia.Generic`.

* **`Parse_nao_detecta_false_positives_lexicos_como_tipos`**
    Mesmo objetivo do Regex: impedir que ruído léxico vire tipo declarado.

* **`Parse_usa_namespace_global_quando_nao_existe_namespace_declarado`**
    Mesmo objetivo do Regex: garantir fallback para `Global`.

**Valor destes testes:**
Comprovam que o parser textual cobre mais padrões relacionais que o Regex, distingue tipos de referência e mantém proteção contra ruído léxico.

---

## 3. Testes do HybridSelectiveParser

**Ficheiro:** `RefactorScope.Tests/Parsing/HybridSelectiveParserTests.cs`



### Coberturas Realizadas

* **`Parse_quando_regex_falha_e_textual_sobrevive_retorna_fallback_triggered`**
    Valida o cenário de fallback principal do parser híbrido.
    *Verifica:* Status `FallbackTriggered`, `UsedFallback = true`, modelo proveniente do parser textual e ParserName indicando fallback para `CSharpTextual`.

* **`Parse_quando_regex_e_textual_falham_retorna_failed`**
    Valida falha total.
    *Verifica:* Status `Failed`, `UsedFallback = false`, `Model = null`.

* **`Parse_quando_regex_sobrevive_e_textual_falha_retorna_baseline_regex`**
    Valida preservação do baseline Regex quando só ele sobrevive.
    *Verifica:* Uso do modelo Regex, não marca fallback, nome do parser híbrido permanece estável.

* **`Parse_quando_ambos_sobrevivem_nao_duplica_tipos_iguais`**
    Valida deduplicação de tipos quando Regex e Textual encontram o mesmo tipo.

* **`Parse_quando_ambos_sobrevivem_nao_duplica_referencias_iguais`**
    Valida deduplicação de referências idênticas entre os dois modelos.

**Valor destes testes:**
Protegem a lógica central do parser híbrido: fallback correto, falha total correta, preservação do baseline e merge sem duplicação.

---

## 4. Testes do StructuralTokenGuard

**Ficheiro:** `RefactorScope.Tests/Parsing/StructuralTokenGuardTests.cs`

### Coberturas Realizadas

* **`Rejeita_tokens_invalidos_como_tipos_declarados`**
    Valida bloqueio de palavras reservadas ou tokens estruturalmente inválidos (*Ex: misuse, summary, remarks, param, class, return, namespace*).

* **`Aceita_identificadores_validos_como_tipos_declarados`**
    Valida aceitação de nomes plausíveis de tipos (*Ex: Program, PrimeAnalyzer, _SyntheticType, MeuServico*).

* **`Rejeita_identificadores_invalidos_ou_fora_do_padrao_de_tipo`**
    Valida rejeição de padrões léxicos problemáticos (*Ex: vazio, espaços, início com número, hífen, espaço interno, camelCase*).

**Valor destes testes:**
O componente funciona como uma proteção lexical de baixo nível. Os testes garantem que ruído textual não seja promovido indevidamente a entidade estrutural.

---

## 5. Testes do Módulo Arena

**Ficheiro:** `RefactorScope.Tests/Parsing/Arena/ParserArenaProjectResultTests.cs`
*(Observação: este ficheiro contém testes para múltiplas classes do módulo Arena).*



### 5.1 ParserArenaProjectResult
* **`OrderedRuns_ShouldSortByStrategyOrder_ThenParserName`**: Valida ordenação estável de apresentação.
* **`HasFailures_ShouldReturnTrue_WhenAnyRunFailed` / `HasFailures_ShouldReturnFalse_WhenNoRunFailed`**: Valida detecção de falhas e cenários saudáveis.
* **`TotalRuns_ShouldReturnRunCount`**: Valida contagem simples de runs.
* **`BestRun_ShouldReturnNull_WhenNoRunsExist`**: Valida comportamento sem dados.
* **`BestRun_ShouldPreferHigherComparativeScore`**: Valida prioridade principal do melhor run.
* **`BestRun_ShouldUseConfidenceAsTieBreaker`**: Valida desempate por confiança.
* **`BestRun_ShouldUseTypeCountThenReferenceCountThenExecutionTimeAsTieBreakers`**: Valida a cadeia de desempate do melhor run (`ComparativeScore` -> `Confidence` -> `TypeCount` -> `ReferenceCount` -> `ExecutionTime`).

### 5.2 ParserArenaRunResult
* **`FromParserResult_ShouldMapAllFields`**: Valida mapeamento integral de um `IParserResult` real.
* **`FromParserResult_ShouldUseDefaults_WhenModelAndStatsAreNull`**: Valida defaults seguros.
* **`FromParserResult_ShouldMapErrorMessage_WhenErrorExists`**: Valida propagação de erro textual.
* **`FromException_ShouldCreateFailedRun`**: Valida criação de run falho a partir de exceção.

### 5.3 ParserArenaScoreCalculator
* **`ApplyScores_ShouldThrow_WhenProjectResultIsNull` / `ApplyScores_ShouldDoNothing_WhenNoRunsExist`**: Valida guard clauses básicas.
* **`ApplyScores_ShouldAssignZeroToFailedRuns`**: Valida penalização máxima de falha.
* **`ApplyScores_ShouldRewardBetterStructuralCoverageAndSpeed`**: Valida impacto positivo de cobertura estrutural e velocidade.
* **`ApplyScores_ShouldPenalizeFallbackUsage`**: Valida penalidade por uso de fallback.
* **`ApplyScores_ShouldDifferentiateStatusWeights`**: Valida a hierarquia de status (`Success` > `PlausibilityWarning` > `FallbackTriggered`).
* **`ApplyScores_ShouldRoundToTwoDecimals`**: Valida arredondamento final do score.

### 5.4 ParserArenaOrchestrator
* **`ExecuteBatch_ShouldThrow_...`**: Valida entradas inválidas e inexistência de diretórios.
* **`ExecuteBatch_ShouldReturnEmptyList_WhenBatchDirectoryHasNoProjects`**: Valida comportamento em batch vazio.

**Gap atual mapeado:**
No MVP, o `ParserArenaOrchestrator` ainda possui cobertura parcial de fluxo porque resolve parsers concretos diretamente (débito técnico mapeado para pós-MVP em ADR).

---

## 6. Testes do StructuralCandidateAnalyzer

**Ficheiro:** `RefactorScope.Tests/Structural/StructuralCandidateAnalyzerTests.cs`

### Coberturas Realizadas

* **`Classe_orfa_entra_como_structural_candidate`**: Valida inclusão de classes órfãs.
* **`Classe_referenciada_nao_entrap_como_structural_candidate`**: Valida que tipos já referenciados não entram.
* **`Program_nao_entra_como_structural_candidate`**: Valida exclusão explícita do `Program`.
* **`Interface_nao_entra_como_structural_candidate` / `Record_nao_entra_como_...`**: Valida exclusão estrutural.
* **`Tipos_de_padrao_estrutural_conhecido_nao_entram_como_candidate`**: Valida exclusão de padrões nominais conhecidos (`Dto`, `Model`, `Contract`, `Request`, `Response`).

**Valor destes testes:**
Formalizam o filtro heurístico do analisador estrutural e ajudam a evitar falsos positivos em classes esperadas ou arquiteturalmente justificadas.

---

## 7. Testes do StructuralCandidateRefinementAnalyzer

**Ficheiro:** `RefactorScope.Tests/Structural/StructuralCandidateRefinementAnalyzerTests.cs`



### Coberturas Realizadas

* **`Refinement_desligado_retorna_lista_vazia`**: Valida que o refinamento respeita a configuração.
* **`Tipo_protegido_por_padrao_arquitetural_recebe_probabilidade_zero`**: Valida proteção total para tipos arquiteturalmente justificáveis.
* **`Tipo_registrado_em_di_reduz_probabilidade`**: Valida redução de probabilidade quando o tipo aparece em registo de DI (`Probability = 0.15`, `DiDetected = true`).
* **`Tipo_com_interface_correspondente_e_referencia_polimorfica_reduz_probabilidade`**: Valida redução quando existe interface correspondente e uso polimórfico (`Probability = 0.25`, `InterfaceDetected = true`).
* **`Tipo_referenciado_em_program_cs_recebe_probabilidade_zero`**: Valida proteção específica para bootstrap/top-level em `Program.cs` (`Probability = 0.0`).

**Valor destes testes:**
Garantem que a camada probabilística interprete corretamente as heurísticas arquiteturais (Injeção de Dependência, Polimorfismo, Bootstrap).

---

## 8. Resumo do Valor Arquitetural da Suíte

A suíte atual não serve apenas para aumentar cobertura. Ela regista explicitamente contratos importantes do sistema:
* O que um parser mínimo deve detetar.
* O que nunca deve ser interpretado como tipo.
* Como o parser híbrido se deve comportar diante de falhas.
* Como o Arena ordena, compara e escolhe runs.
* Como candidatos estruturais são classificados e refinados.

Os testes já funcionam como **rede de segurança contra regressão**, **documentação executável** e **base de confiança** para a evolução pós-MVP.

---

## 9. Limitações Atuais Conhecidas

* **Arena Orchestrator:** Não possui testes completos de fluxo com simulação controlada de múltiplas estratégias devido ao acoplamento atual com a resolução concreta dos parsers.
    * *Classificação:* Débito técnico de testabilidade.
    * *Urgência:* Baixa no MVP, Média no pós-MVP.
* **Parsers Reais:** A suíte cobre bem cenários sintéticos e controlados, mas pode evoluir para projetos maiores, nested types, partial classes, interfaces genéricas complexas e múltiplos namespaces.

---

## 10. Próximos Passos Sugeridos

* **Parsing:**
    * Expandir cenários de referência complexa.
    * Testar nested types e partial classes.
    * Testar ambiguidades léxicas adicionais.
* **Arena (pós-MVP):**
    * Desacoplar resolução de parser do orchestrator.
    * Ampliar testes de fluxo completo do batch comparativo.
* **Structural:**
    * Adicionar cenários com mais ruído arquitetural real.
    * Validar thresholds probabilísticos com corpus maior.

---

## 11. Conclusão

A suíte de testes atual do RefactorScope cobre com boa qualidade os pontos mais sensíveis do MVP: parsing básico e enriquecido, proteção contra falsos positivos léxicos, composição híbrida de parsers, comparativo Arena, e detecção/refinamento estrutural.