# Documentação da Suíte de Testes do RefactorScope

## Objetivo

Este documento descreve o estado atual da suíte de testes automatizados do RefactorScope, consolidando a cobertura funcional do MVP e a evolução recente da proteção dos módulos mais sensíveis do sistema.

O objetivo da suíte é:

- validar o comportamento funcional esperado dos componentes centrais
- reduzir regressões durante refatorações
- explicitar contratos arquiteturais importantes
- fornecer documentação executável do comportamento do sistema

---

## Visão Geral

A suíte atual cobre, com diferentes níveis de profundidade, os seguintes domínios principais:

1. **Parsers C#**
   - `CSharpRegexParser`
   - `CSharpTextualParser`
   - `HybridSelectiveParser`

2. **Proteção Léxica**
   - `StructuralTokenGuard`

3. **Arena / Parser Comparison**
   - `ParserArenaProjectResult`
   - `ParserArenaRunResult`
   - `ParserArenaScoreCalculator`
   - `ParserArenaOrchestrator`
   - infraestrutura pública do `ParserArenaCliRunner`

4. **Camada CLI**
   - `StartupExecutionPlanSelector`
   - cobertura estrutural do fluxo de seleção de modo e parser

5. **Análise Estrutural**
   - `StructuralCandidateAnalyzer`
   - `StructuralCandidateRefinementAnalyzer`
   - `ConsolidatedReport`
   - snapshots estruturais e breakdowns

6. **Core / Results / Reporting**
   - `ConsolidatedReport`
   - `ReportSnapshot`
   - `ReportSnapshotBuilder`
   - extensões de snapshot e breakdown estrutural

7. **Exportação**
   - exportadores HTML principais em cobertura básica
   - exportadores Markdown
   - dumps JSON
   - datasets CSV
   - adapters de exportação

8. **Execution / Dump**
   - `DumpStrategyResolver`
   - `GlobalDumpStrategy`
   - `SegmentedDumpStrategy`

9. **Statistics**
   - `ValidationEngine`
   - `StatisticsReport`
   - `ParsingConfidence`
   - `MetricsStatisticsSummary`

10. **Estimation**
    - `EffortEstimator`
    - `RefactorClassifier`
    - `RDICalculator`
    - `StructuralRiskModel`
    - `CouplingPressureModel`
    - `SizePressureModel`
    - `RefactorDifficultyIndex`
    - `EffortEstimate`

11. **Infrastructure**
    - `ConfigLoader`
    - `ConfigValidator`
    - `LayerRuleEvaluator`
    - `CrashLogger`
    - partes utilitárias do `TerminalRenderer`

---

## Estado Executivo Atual da Cobertura

### Cobertura Global

- **Cobertura global do código-fonte:** `46.53%`
- **Linhas cobertas:** `14.812`
- **Linhas totais:** `31.832`

### Leitura Executiva

A cobertura atual do RefactorScope já protege bem os motores analíticos e matemáticos do sistema. O número global ainda é pressionado por camadas extensas de exportação visual e renderização HTML, que concentram muito volume e alta complexidade ciclomática.

Em termos práticos:

- o **núcleo analítico do MVP** está significativamente mais protegido
- os **parsers e a arena comparativa** já possuem blindagem funcional relevante
- os **módulos de Estimation e Statistics** atingiram maturidade alta
- a média global ainda é achatada por **exporters visuais, renderers e dashboards de alto volume**

---

## Cobertura por Domínio (Nível Raiz)

| Domínio | Linhas (Total) | Linhas (Cobertas) | Cobertura (%) | Status |
|---|---:|---:|---:|---|
| RefactorScope.Estimation | 206 | 188 | 91.26% | 🟢 Bom |
| RefactorScope.Statistics | 84 | 66 | 78.57% | 🟡 Atenção |
| RefactorScope.Execution | 148 | 98 | 66.22% | 🟡 Atenção |
| RefactorScope.Parsers | 2.396 | 1.507 | 62.90% | 🟡 Atenção |
| RefactorScope.Core | 3.514 | 1.718 | 48.89% | 🔴 Crítico |
| RefactorScope.Exporters | 22.025 | 10.253 | 46.55% | 🔴 Crítico |
| RefactorScope.Analyzers | 1.763 | 588 | 33.35% | 🔴 Crítico |
| RefactorScope.CLI | 1.004 | 284 | 28.29% | 🔴 Crítico |
| RefactorScope.Infrastructure | 692 | 110 | 15.90% | 🔴 Crítico |

### Interpretação

Essa distribuição mostra que a cobertura não está “rasteira e pulverizada”, mas sim concentrada nos módulos de maior valor metodológico do MVP:

- parsing
- arena
- estimation
- statistics
- snapshots e consolidação estrutural

As áreas ainda frágeis estão majoritariamente em:

- renderização HTML complexa
- infraestrutura visual
- exportadores extensos
- regras auxiliares menos centrais ao núcleo analítico

---

## Top 5 e Bottom 5 Namespaces

### Top 5 (mais seguros)

| Namespace | Cobertura |
|---|---:|
| RefactorScope.Execution.Dump.Strategies | 100% |
| RefactorScope.Execution.Dump | 100% |
| RefactorScope.Estimation.Scoring | 100% |
| RefactorScope.Estimation.Classification | 100% |
| RefactorScope.Exporters.Dumps | 96.67% |

### Bottom 5 (risco imediato)

| Namespace | Cobertura |
|---|---:|
| RefactorScope.Core.Structure | 0% |
| RefactorScope.Exporters.Datasets | 0% |
| RefactorScope.Exporters.Dashboards.Renderers... | 0% |
| RefactorScope.Core.Reporting.Export | 0% |
| RefactorScope.Analyzers.Solid.Rules | 0% |

### Interpretação

Os namespaces mais protegidos hoje são justamente os blocos menores, determinísticos e fáceis de isolar. Já os de menor cobertura concentram partes mais visuais, especializadas ou ainda não priorizadas no MVP.

---

## Classes Críticas por Volume

As classes abaixo concentram grande volume de código e continuam puxando a média global para baixo:

| Classe | Namespace | Linhas (Total) | Cobertura (%) |
|---|---|---:|---:|
| ModuleRouteMapRenderer | RefactorScope.Exporters.Dashboards.Renderers | 1680 | 0.00% |
| SimpleStructureMapRenderer | RefactorScope.Exporters.Dashboards.Renderers | 1638 | 0.00% |
| StructuralInventoryExporter | RefactorScope.Exporters.Dashboards | 1418 | 3.95% |
| DashboardMetricsCalculator | RefactorScope.Exporters.Projections | 958 | 35.49% |
| QualityDashboardExporter | RefactorScope.Exporters.Dashboards | 888 | 0.00% |

### Interpretação

Essas classes representam o principal gargalo estatístico atual. Elas não significam necessariamente fragilidade proporcional do MVP, mas distorcem a média global por serem:

- extensas
- altamente ramificadas
- ricas em montagem textual/visual
- menos determinísticas que os motores analíticos

---

## Risk Hotspots (Complexidade)

| Classe | Método | Complexidade Ciclomática | Risco |
|---|---|---:|---|
| ParserArenaDashboardExporter | GenerateHtml(...) | 108 | 🔥 Extremo |
| TableRenderer | Render(...) | 108 | 🔥 Extremo |
| FileSize | GetSuffix() | 79 | 🔥 Extremo |
| CSharpRegexParser | PrepareStructuralScanSource(...) | 72 | 🔥 Extremo |
| TerminalRenderer | ResolveModuleColor(...) | 66 | 🔥 Extremo |

### Interpretação

Esses hotspots são os principais candidatos a:

- testes adicionais de fumaça
- testes de snapshot textual
- eventual refatoração pós-MVP
- decomposição por responsabilidade

---

## 1. Testes dos Parsers C#

### 1.1 CSharpRegexParser

**Coberturas realizadas:**

- projeto simples retorna modelo válido
- múltiplos tipos no mesmo projeto
- detecção de referência entre tipos
- proteção contra falsos positivos léxicos
- fallback para namespace `Global`

**Valor arquitetural:**

Esses testes estabelecem o contrato mínimo do parser Regex e o protegem especialmente contra:
- regressões de extração básica
- ruído léxico
- quebra de fallback de namespace

### 1.2 CSharpTextualParser

**Coberturas realizadas:**

- fluxo básico do parser textual
- múltiplos tipos
- referências por instância e menção
- referências por `typeof`, `nameof` e genéricos
- proteção contra falsos positivos
- fallback para namespace `Global`

**Valor arquitetural:**

Garante que o parser textual vai além do regex puro e mantém consistência sem sacrificar proteção contra ruído.

### 1.3 HybridSelectiveParser

**Coberturas realizadas:**

- fallback para textual quando regex falha
- falha total quando ambos falham
- preservação do baseline regex
- merge sem duplicação de tipos
- merge sem duplicação de referências

**Valor arquitetural:**

Protege o coração do parsing híbrido:
- fallback correto
- merge consistente
- comportamento estável em cenários mistos

---

## 2. Proteção Léxica

### StructuralTokenGuard

**Coberturas realizadas:**

- rejeição de tokens inválidos como tipos
- aceitação de identificadores plausíveis
- rejeição de padrões lexicais inseguros

**Valor arquitetural:**

Funciona como uma barreira de baixo nível contra ruído textual promovido indevidamente a entidade estrutural.

---

## 3. Arena

### Coberturas principais

- ordenação de runs
- seleção de `BestRun`
- desempates por score/confiança/tipos/referências/tempo
- score zero para falhas
- penalidade para fallback
- diferenciação por status
- arredondamento de score
- validação de batch path e batch vazio
- validações estruturais do `ParserArenaCliRunner`

### Gap conhecido

O `ParserArenaOrchestrator` ainda possui cobertura parcial de fluxo completo porque resolve parsers concretos diretamente.

**Classificação:** débito de testabilidade  
**Urgência:** baixa no MVP, média no pós-MVP

---

## 4. CLI

### StartupExecutionPlanSelector

A camada CLI ganhou proteção para o fluxo de inicialização e seleção de modos, com foco em:

- resolução do escopo
- seleção de modo de execução
- comportamento quando o seletor interativo está desligado
- comportamento em single parser vs comparative mode

**Valor arquitetural:**

Esse bloco ajuda a blindar a infraestrutura de entrada do sistema sem exigir mudanças invasivas no código congelado do MVP.

---

## 5. Structural Analysis

### StructuralCandidateAnalyzer

**Coberturas realizadas:**

- classe órfã entra como structural candidate
- classe referenciada não entra
- `Program` não entra
- interfaces e records não entram
- padrões estruturais conhecidos não entram

### StructuralCandidateRefinementAnalyzer

**Coberturas realizadas:**

- refinamento desligado retorna vazio
- proteção por padrão arquitetural
- redução via DI
- redução via interface e polimorfismo
- proteção por referência em `Program.cs`

### ConsolidatedReport e breakdowns

Também foram cobertos:

- acesso tipado por `GetResult<T>()`
- structural candidates
- unresolved candidates
- pattern similarity
- cálculo de breakdown estrutural
- taxa de unresolved
- snapshots estruturais derivados do relatório consolidado

**Valor arquitetural:**

Essa camada formaliza a semântica central do pipeline estrutural do RefactorScope:
`Structural Candidates -> Refinement -> Unresolved / Pattern Similarity`

---

## 6. Statistics

### ValidationEngine

**Coberturas realizadas:**

- retorno nulo quando statistics está desabilitado
- proteção contra divisão por zero
- cálculo de `ParsingConfidence`
- cálculo de `MeanCoupling`
- cálculo de `UnresolvedCandidateRatio`
- cálculo de `NamespaceDriftRatio`
- modo de execução não bloqueante

### Statistics Models

Também receberam cobertura:
- `ParsingConfidence`
- `MetricsStatisticsSummary`
- `StatisticsReport`

**Valor arquitetural:**

O módulo estatístico já está suficientemente protegido para o MVP e funciona como camada observacional estável, sem acoplamento crítico ao fluxo principal.

---

## 7. Estimation

### Coberturas realizadas

- `RefactorClassifier`
- `StructuralRiskModel`
- `CouplingPressureModel`
- `SizePressureModel`
- `RDICalculator`
- `RefactorDifficultyIndex`
- `EffortEstimator`
- `EffortEstimate`

### Valor arquitetural

Esse foi um dos maiores avanços da fase atual. O domínio de estimation saiu de quase nenhuma cobertura para um estado altamente confiável, com proteção tanto dos modelos matemáticos quanto da leitura heurística de dificuldade.

---

## 8. Core Reporting / Snapshots

### Coberturas realizadas

- `ReportSnapshot`
- `ReportSnapshotBuilder`
- `ConsolidatedReportSnapshotExtensions`
- snapshots executivos de:
  - Parsing
  - Structural
  - Architectural
  - Quality
  - Effort

**Valor arquitetural:**

Essa camada é importante porque serve como ponte entre:
- núcleo analítico
- exportadores Markdown
- futura documentação executiva
- integração com GitHub Pages e possíveis exports JSON

---

## 9. Exporters

### O que já está coberto

Cobertura básica e/ou estrutural foi adicionada para:

- exportadores Markdown
- dumps JSON
- execução/dump strategies
- adapters de exportação
- shell visual
- parte dos exporters HTML principais
- snapshots e métricas de suporte à visualização

### O que ainda pesa

Os maiores gargalos ainda são:

- `StructuralInventoryExporter`
- `QualityDashboardExporter`
- `ModuleRouteMapRenderer`
- `SimpleStructureMapRenderer`
- renderers P5/CSS mais extensos

**Leitura correta:**

A camada de exportação ainda concentra muito volume, então a cobertura global parece menor do que a robustez real do MVP.

---

## 10. Infrastructure

### Coberturas realizadas

- `ConfigLoader`
- `ConfigValidator`
- `LayerRuleEvaluator`
- `CrashLogger`
- partes de `TerminalRenderer`

### Leitura

A cobertura ainda é baixa no agregado, mas já não está zerada e os utilitários mais sensíveis do MVP receberam proteção mínima funcional.

---

## 11. Limitações Atuais Conhecidas

- fluxos completos de alguns exporters HTML ainda não estão profundamente testados
- renderers visuais grandes continuam puxando a média global
- algumas classes muito extensas ainda carecem de testes de fumaça ou snapshot
- `TerminalRenderer` e renderers complexos possuem alta complexidade e custo de teste maior que seu valor imediato para o MVP
- regras especializadas de `Analyzers.Solid.Rules` ainda estão praticamente fora da malha de testes

---

## 12. Próximos Passos Sugeridos

### Curto prazo
- adicionar smoke tests em exporters grandes
- criar testes de snapshot textual/HTML para reduzir zonas cegas
- atacar `QualityDashboardExporter` e `StructuralInventoryExporter`

### Pós-MVP
- decompor renderers gigantes
- desacoplar mais pontos do `ParserArenaOrchestrator`
- ampliar cobertura das regras específicas de SOLID
- revisar hotspots com complexidade extrema

---

## 13. Conclusão

A suíte de testes do RefactorScope já protege com boa qualidade o núcleo metodológico do MVP.

Hoje, a principal leitura correta não é apenas “46.53% de cobertura global”, mas sim:

- parsing protegido
- arena protegida
- estimation fortemente protegido
- statistics em bom estado
- snapshots e consolidação executiva já confiáveis
- exportação visual ainda em expansão

Isso coloca o projeto numa posição saudável para lançamento de MVP, com clareza objetiva sobre os pontos fortes e os débitos pós-lançamento.