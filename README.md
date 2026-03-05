README.md (PT-BR)
# 🧬 RefactorScope

O **RefactorScope** é uma ferramenta de **análise estrutural estática** projetada para auditar, visualizar e garantir a saúde arquitetural de bases de código C#.

A ferramenta detecta **candidatos estruturais (possível código morto)**, analisa **acoplamento**, avalia **aderência arquitetural** e produz **dashboards estruturais e datasets para BI**.

Seu foco principal é **refatoração arquitetural assistida por métricas**, funcionando como um **scanner de higiene estrutural** para projetos .NET.

---

# 💡 Origem do Projeto

O RefactorScope nasceu de uma necessidade prática: auditar e guiar a refatoração do **Scriptome** — uma ferramenta complexa de análise narrativa desenvolvida em C#.

À medida que o Scriptome crescia, tornou-se necessário:

- detectar **código morto estrutural**
- verificar **isolamento da camada Core**
- medir **acoplamento arquitetural**
- garantir aderência a **SOLID**
- identificar **drift de namespace**
- gerar **dashboards de arquitetura**

O RefactorScope foi então criado como **ferramenta independente e agnóstica**, capaz de analisar qualquer base de código C#.

---

# 🎯 Objetivos do Projeto

O RefactorScope busca oferecer:

- 🔍 Auditoria estrutural rápida de projetos C#
- 🧠 Detecção heurística de código morto
- 🧱 Análise de isolamento arquitetural
- 🔗 Mapeamento de dependências estruturais
- 📊 Dashboards visuais de arquitetura
- 📈 Datasets para BI e análise histórica
- 🤖 Exportação estruturada para análise por IA
- ⚙️ Integração com pipelines de CI/CD

---

# 🏗️ Arquitetura da Ferramenta

O RefactorScope segue princípios de **Clean Architecture**, mantendo os módulos independentes.


RefactorScope
│
├── Core
│ Modelo estrutural, DTOs e datasets
│
├── Parsers
│ Motores de leitura estrutural de código
│
├── Analyzers
│ Módulos de análise arquitetural
│
├── Exporters
│ Geração de dashboards e datasets
│
├── Infrastructure
│ Serviços externos e integração
│
└── CLI
Interface de execução via terminal


## Core

Contém:

- Modelo estrutural do código
- DTOs de resultados consolidados
- datasets estruturais para exportação
- lógica de normalização de métricas

Essa camada é **agnóstica de framework**.

---

# ⚙️ Pipeline de Execução

O fluxo de execução do RefactorScope segue um pipeline determinístico:


1️⃣ Parser
↓
2️⃣ Structural Model
↓
3️⃣ Analyzers
↓
4️⃣ Consolidated Results
↓
5️⃣ Exporters


### Parser

Extrai a estrutura do código:

- classes
- interfaces
- namespaces
- dependências
- localização física

Produz um **grafo estrutural de dependência**.

---

### Analyzers

Os analyzers são **plugáveis** e não possuem responsabilidade de exportação.

Cada analyzer:

- consome o modelo estrutural
- produz um **DTO de resultado**

Exemplos:


StructuralCandidateAnalyzer
CouplingAnalyzer
SolidAnalyzer
CoreIsolationAnalyzer
NamespaceDriftAnalyzer


Esses resultados são posteriormente consolidados.

---

### Consolidated Results

Os resultados dos analyzers são consolidados em um objeto central:


ConsolidatedReport


Esse DTO representa o **estado arquitetural da base analisada**.

---

### Exporters

Exporters **não realizam análise**.

Eles apenas convertem os resultados em formatos úteis:

- dashboards HTML
- Markdown reports
- CSV datasets
- dumps estruturais para IA

---

# ⚙️ Motores de Parsing

O RefactorScope prioriza **velocidade e resiliência**, evitando dependência pesada de AST.

Dois motores estão disponíveis:

---

## CSharpRegexParser (Estável)

Parser baseado em expressões regulares robustas.

Características:

- extremamente rápido
- tolerante a erros
- ideal para CI
- ignora palavras modernas do C# (`record`, `init`, etc.)

---

## CSharpTextualParser (Experimental)

Parser textual com **higienização léxica avançada**.

Etapas:

1. remoção de comentários
2. neutralização de strings
3. leitura estrutural

Evita armadilhas como:


"http://site.com
"


ser interpretado como namespace.

---

# 📊 Métricas Estruturais

## Structural Candidates

Classes sem referências estruturais.

Possíveis causas:

- código morto
- pontos de entrada CLI
- plugins
- wiring via DI
- reflexão

---

## Pattern Similarity

Classes que parecem seguir **padrões arquiteturais conhecidos**.

Exemplos:


Factory
Strategy
Handler
Repository
Dto
Adapter


---

## Unresolved

Candidatos estruturais que **não foram explicados por heurísticas**.

São os melhores indicadores de **código potencialmente morto**.

---

## Namespace Drift

Detecta divergência entre:


namespace declarado
vs
estrutura de pastas


Arquiteturas limpas normalmente seguem:


namespace == estrutura física


---

## Global Namespace

Classes declaradas sem namespace.

Isso reduz modularidade e clareza arquitetural.

---

## Isolated Core

Avalia se a camada Core está corretamente isolada.

Arquitetura saudável:


Core → não depende de infraestrutura
Infra → depende do Core


---

## SOLID Alerts

Detecção heurística de potenciais violações:

- SRP
- LSP
- ISP
- DIP

Essas métricas são **sinais arquiteturais**, não provas definitivas.

---

# 📡 Radar Arquitetural

O dashboard inclui um **radar de saúde arquitetural**.

Valores são normalizados entre **0 e 1**.


0.00 – 0.10 saudável
0.10 – 0.25 atenção moderada
0.25 – 0.50 revisão recomendada
0.50 – 1.00 risco arquitetural


O radar permite identificar rapidamente:

- crescimento de acoplamento
- aumento de candidatos mortos
- deterioração arquitetural

---

# 🧠 Refinamento Heurístico

Para reduzir falsos positivos, o RefactorScope usa duas camadas heurísticas.

## Pattern Signature Library

Detecta padrões conhecidos pelo nome da classe.

Exemplo:


UserRepository
OrderFactory
CommandHandler


---

## Structural Heuristics

Detecta contextos estruturais comuns.

Exemplo:


*.CLI
*.Infrastructure
*.Extensions
*.Options


Esses tipos frequentemente aparecem sem referências diretas.

---

# 📊 Datasets e BI

O RefactorScope gera **datasets estruturais** que podem ser consumidos por ferramentas de BI:

- Power BI
- QuickSight
- dashboards customizados

Esses datasets permitem:

- acompanhar evolução arquitetural
- detectar regressões
- construir métricas históricas

---

# 🤖 Exportação para IA

Os relatórios também podem ser exportados como **dumps estruturais para LLMs**.

Isso permite:

- auditoria assistida por IA
- revisão arquitetural automatizada
- análise de refatoração

---

# ⚙️ Configuração

O comportamento é controlado por `refactorscope.json`.

```json
{
  "RootPath": "C:\\project",
  "Include": [ "src" ],
  "Exclude": [ "bin", "obj", "tests" ],
  "Parser": "CSharpRegex",
  "Analyzers": {
    "zombie": true,
    "zombieRefinement": true,
    "architecture": true,
    "coupling": true,
    "coreIsolation": true,
    "solid": true,
    "fitnessGates": true
  }
}
🚀 Uso
dotnet run --project RefactorScope.CLI

ou

RefactorScope analyze
🔮 Roadmap

Evoluções planejadas:

Parser baseado em Roslyn

Visualizações com ScottPlot

análise histórica de arquitetura

modo biblioteca plugável

integração CI/CD avançada

análise multi-linguagem

🧩 Integração com Scriptome

O RefactorScope foi criado inicialmente para auditar o Scriptome, uma plataforma de análise narrativa.

Hoje ele evolui como ferramenta independente de engenharia de software.

⚠️ Limitações

A análise é estrutural e estática.

Mecanismos de runtime podem gerar falsos positivos:

Dependency Injection

Reflection

Framework wiring

Plugin systems

Sempre é necessária revisão humana.

📜 Licença

Projeto aberto para uso educacional, pesquisa e auditoria arquitetural.


---

# README.ENG.MD (English)

```markdown
# 🧬 RefactorScope

**RefactorScope** is a **static structural analysis tool** designed to audit, visualize, and maintain the architectural health of C# codebases.

It detects **structural candidates (potential dead code)**, evaluates **architectural coupling**, checks **layer isolation**, and generates **architectural dashboards and datasets for BI analysis**.

The main goal is to support **metric-driven architectural refactoring**.

---

# 💡 Project Origin

RefactorScope emerged from a real-world need: auditing and guiding the architectural refactor of **Scriptome**, a complex narrative analysis engine written in C#.

As Scriptome grew, it became necessary to:

- detect structural dead code
- verify Core layer isolation
- measure architectural coupling
- enforce SOLID adherence
- detect namespace drift
- produce architecture dashboards

RefactorScope was created as an **independent, language-agnostic architectural scanner** for C# codebases.

---

# 🎯 Goals

RefactorScope aims to provide:

- fast structural auditing of C# projects
- heuristic dead code detection
- architectural isolation analysis
- structural dependency mapping
- architecture dashboards
- BI-ready datasets
- structured exports for AI analysis
- CI/CD architecture monitoring

---

# 🏗️ Architecture

RefactorScope follows **Clean Architecture principles** with strongly separated modules.


RefactorScope
│
├── Core
│ Domain model and datasets
│
├── Parsers
│ Code structure extraction
│
├── Analyzers
│ Architectural analysis modules
│
├── Exporters
│ Dashboards and report generators
│
├── Infrastructure
│ External integrations
│
└── CLI
Command-line interface


---

# ⚙️ Execution Pipeline

The analysis pipeline follows a deterministic flow:


Parser
↓
Structural Model
↓
Analyzers
↓
Consolidated Results
↓
Exporters


---

# Parser

Extracts structural information:

- classes
- interfaces
- namespaces
- dependencies
- physical location

Produces a **structural dependency graph**.

---

# Analyzers

Analyzers are **plug-in modules** that perform architectural analysis.

Each analyzer:

- consumes the structural model
- produces a **result DTO**

Examples:


StructuralCandidateAnalyzer
CouplingAnalyzer
SolidAnalyzer
CoreIsolationAnalyzer
NamespaceDriftAnalyzer


---

# Consolidated Results

All analyzer outputs are aggregated into:


ConsolidatedReport


This represents the **architectural state of the analyzed project**.

---

# Exporters

Exporters generate output formats without performing analysis.

Supported outputs include:

- HTML dashboards
- Markdown reports
- CSV datasets
- AI-ready structural dumps

---

# ⚙️ Parsing Engines

RefactorScope prioritizes **speed and resilience**, avoiding heavy AST dependencies.

Two parsing engines are available.

---

## CSharpRegexParser (Stable)

Regex-based parser designed for performance.

Features:

- very fast
- resilient to syntax errors
- CI-friendly
- ignores modern keywords (`record`, `init`)

---

## CSharpTextualParser (Experimental)

Advanced textual parser with lexical sanitization.

Steps:

1. comment removal
2. string neutralization
3. structural extraction

Prevents traps such as URLs being interpreted as namespaces.

---

# 📊 Structural Metrics

## Structural Candidates

Classes with zero structural references.

Possible causes:

- dead code
- CLI entry points
- plugins
- DI wiring
- reflection

---

## Pattern Similarity

Classes matching known architectural patterns.

Examples:


Factory
Strategy
Handler
Repository
Dto
Adapter


---

## Unresolved

Structural candidates not explained by heuristics.

These are the **strongest indicators of dead code**.

---

## Namespace Drift

Detects mismatch between:


declared namespace
vs
physical folder structure


Clean architectures usually align both.

---

## Global Namespace

Classes declared without a namespace.

This reduces modular clarity.

---

## Isolated Core

Evaluates whether the Core layer remains isolated.

Healthy architecture:


Core → independent
Infra → depends on Core


---

## SOLID Alerts

Heuristic detection of possible violations:

- SRP
- LSP
- ISP
- DIP

These are **architectural signals**, not proofs.

---

# 📡 Architectural Radar

The dashboard includes a **radar chart** summarizing architectural health.

Values normalized between **0 and 1**.


0.00 – 0.10 healthy
0.10 – 0.25 moderate signal
0.25 – 0.50 architectural attention
0.50 – 1.00 high risk


---

# 🧠 Heuristic Refinement

Two layers reduce false positives.

## Pattern Signature Library

Detects known design patterns from class names.

Example:


OrderRepository
CommandHandler
UserFactory


---

## Structural Heuristics

Detects framework-related structural contexts.

Examples:


*.CLI
*.Infrastructure
*.Extensions
*.Options


---

# 📊 BI Datasets

RefactorScope generates datasets suitable for BI tools:

- Power BI
- QuickSight
- custom analytics

This enables **historical architecture tracking**.

---

# 🤖 AI Integration

Reports can be exported as **AI-ready structural dumps**.

Use cases:

- AI-assisted architecture review
- automated refactor suggestions
- design audits

---

# ⚙️ Configuration

Behavior is controlled through `refactorscope.json`.

```json
{
  "RootPath": "C:\\project",
  "Include": [ "src" ],
  "Exclude": [ "bin", "obj", "tests" ],
  "Parser": "CSharpRegex"
}
🚀 Usage
dotnet run --project RefactorScope.CLI

or

RefactorScope analyze
🔮 Roadmap

Future improvements:

Roslyn-based parser

ScottPlot visualizations

historical architecture analysis

plug-in library mode

advanced CI integration

multi-language analysis

⚠️ Limitations

The analysis is purely structural and static.

Runtime mechanisms may generate false positives:

Dependency Injection

Reflection

framework wiring

plugin architectures

Human review is always required.

📜 License

Open for educational use, research and architectural auditing.