# Registos de Decisão de Arquitetura (ADRs) - RefactorScope

## ADR-001 — Arquitetura Fundamental e *Flow Pipeline* do RefactorScope
**Status:** Accepted

### Contexto
O RefactorScope foi concebido como uma ferramenta de análise estrutural para apoiar processos de refatoração arquitetural, com foco em previsibilidade, modularidade e diagnóstico *offline*. Ferramentas deste tipo sofrem frequentemente de alguns problemas recorrentes:
* Acoplamento entre módulos.
* *Pipelines* implícitos e difíceis de auditar.
* Mistura entre *parsing*, análise e exportação.
* Crescimento descontrolado de escopo.

O objetivo do RefactorScope 1.x é manter um *pipeline* explícito, determinístico e modular.

### Decisão
O RefactorScope adota uma arquitetura baseada num **Pipeline Orquestrado de Extração, Análise e Consolidação**.

**Fluxo principal:**
```text
Configuração / CLI
   ↓
Parser
   ↓
Modelo Estrutural
   ↓
Orquestrador
   ↓
[ Analyzers Independentes ]
   ↓
Resultados
   ↓
Consolidação
   ↓
Exportação
```

### Descrição do *Flow Pipeline*

#### 1. Configuração
A execução começa na CLI e no ficheiro `refactorscope.json`, que definem:
* Escopo de análise.
* Inclusão/exclusão de pastas.
* *Analyzers* ativos.
* Regras de camada.
* Limites (*thresholds*) e *gates*.

#### 2. *Parsing*
O código-fonte é processado por um *parser* estrutural. O objetivo desta etapa não é compilar, mas extrair um modelo estrutural suficientemente confiável para análise.

**Saídas típicas:**
* Ficheiros.
* *Namespaces*.
* Tipos.
* Referências.

#### 3. Modelo Estrutural
O *parser* gera um modelo intermediário agnóstico, usado como entrada para toda a fase analítica.
Este modelo desacopla:
* O código-fonte real.
* Os *analyzers*.
* Os *exporters*.

#### 4. Análise
Os *analyzers* operam sobre o modelo estrutural e calculam sinais como:
* Candidatos estruturais.
* Risco de *zombie code*.
* *Coupling* (acoplamento).
* Métricas arquiteturais.
* Refinamentos heurísticos.
* *Gates* de qualidade.

#### 5. Consolidação
Os resultados emitidos pelos *analyzers* são reunidos num relatório consolidado, sem que os *analyzers* dependam entre si.

#### 6. Exportação
A camada de exportação converte os resultados em artefatos de consumo humano ou analítico, como:
* *Dashboards* HTML.
* Relatórios Markdown.
* *Datasets* CSV.
* *Dumps* auxiliares.

### Princípios Arquiteturais
* *Pipeline* explícito.
* *Analyzers* independentes.
* Comunicação via *Result Objects*.
* Execução previsível.
* Núcleo desacoplado de UI.
* Separação entre extração, análise e saída.
* Escopo 1.x centrado em análise estrutural.

### Consequências
**Positivas:**
* Previsibilidade de execução.
* Facilidade de expansão.
* Menor acoplamento entre módulos.
* Maior auditabilidade do fluxo.

**Negativas:**
* Mais objetos intermediários.
* Necessidade de contratos estáveis.
* Maior disciplina arquitetural para evitar atalhos.

---

## ADR-002 — Modelo Estrutural Agnóstico como Contrato Central
**Status:** Accepted

### Contexto
Analisar diretamente código C# dentro dos *analyzers* acoplaria o sistema ao *parser* e reduziria a capacidade de evolução futura. O projeto precisava de um contrato intermediário que permitisse:
* Trocar estratégias de *parsing*.
* Suportar novas linguagens no futuro.
* Manter *analyzers* independentes do *parser* concreto.

### Decisão
O RefactorScope utiliza um **Modelo Estrutural Agnóstico** como contrato central entre *parsing* e análise.

**Fluxo:**
```text
Código Fonte
   ↓
Parser
   ↓
Modelo Estrutural
   ↓
Analyzers / Exporters
```

### Componentes Principais

**ArquivoInfo**
Representa um ficheiro analisado. Contém tipicamente:
* Caminho relativo.
* *Namespace*.
* Tipos declarados.
* Código-fonte associado.

**TipoInfo**
Representa entidades estruturais como:
* Classes.
* Interfaces.
* Records.
* Structs.

Contém:
* Nome.
* *Namespace*.
* Tipo lógico.
* Ficheiro de declaração.
* Referências associadas.

**ReferenciaInfo**
Representa dependências ou relações detetadas entre tipos. Exemplos:
* Menção.
* Instanciação.
* Uso genérico.
* `typeof`.
* `nameof`.

### Princípios
* *Analyzers* operam sobre estrutura, não sobre sintaxe crua.
* *Parser* é substituível.
* Modelo é neutro em relação à linguagem.
* *Outputs* não dependem diretamente de um *parser* concreto.

### Consequências
**Positivas:**
* Desacoplamento entre *parsing* e análise.
* Suporte futuro a múltiplos *parsers*.
* Maior legibilidade do *pipeline*.
* Melhor testabilidade dos *analyzers*.

**Negativas:**
* O modelo precisa de ser mantido com rigor.
* Limitações de deteção do *parser* impactam o restante do *pipeline*.

---

## ADR-003 — Execução Independente dos *Analyzers* e Consolidação Central
**Status:** Accepted

### Contexto
Uma das fontes mais comuns de deriva arquitetural em ferramentas analíticas é o acoplamento entre regras de análise. Quando os *analyzers* passam a depender uns dos outros, surgem problemas como:
* Ordem implícita de execução.
* Efeitos colaterais.
* Dificuldade de teste.
* Crescimento acidental de dependências.

### Decisão
Todos os *analyzers* do RefactorScope devem operar de forma isolada, recebendo apenas o contexto de análise e emitindo um resultado próprio.

**Contrato:**
```csharp
public interface IAnalyzer
{
    IAnalysisResult Analyze(AnalysisContext context);
}
```

### Regras
**Um *analyzer*:**
* Recebe `AnalysisContext`.
* Executa isoladamente.
* Retorna `IAnalysisResult`.

**Um *analyzer* não deve:**
* Consultar diretamente o resultado de outro *analyzer*.
* Aceder ao sistema de ficheiros (*filesystem*) como parte da análise.
* Modificar o contexto partilhado.
* Depender de ordem implícita externa.

### Consolidação
A responsabilidade de reunir os resultados pertence ao Orquestrador Central, que produz o `ConsolidatedReport`.

O orquestrador:
* Seleciona *analyzers* ativos.
* Executa *analyzers*.
* Coleta resultados.
* Consolida saídas.

O orquestrador não deve conter lógica analítica de domínio que pertença aos *analyzers*.

### Consequências
**Positivas:**
* *Analyzers* testáveis isoladamente.
* *Pipeline* previsível.
* Menos acoplamento lateral.
* Evolução mais segura do sistema.

**Negativas:**
* Resultados cruzados precisam de ser modelados explicitamente.
* Análises compostas exigem uma etapa clara de consolidação.

---

## ADR-004 — Precisão Estrutural e Heurística Probabilística Acima de Cobertura Total
**Status:** Accepted

### Contexto
Na análise estrutural estática, tentar cobrir todos os cenários possíveis costuma gerar um aumento de falsos positivos, especialmente em arquiteturas modernas com:
* Injeção de Dependências (DI).
* *Strategy* / *Factory*.
* Uso via interfaces.
* *Bootstrap* no `Program.cs`.
* Padrões indiretos de ativação.

No contexto do RefactorScope, uma ferramenta útil precisa de ser conservadora o suficiente para não acusar como problema aquilo que faz parte da arquitetura esperada.

### Decisão
O RefactorScope prioriza:
**Precisão estrutural acima de cobertura universal.**

Isso implica aceitar conscientemente que:
* Alguns falsos negativos existirão.
* A cobertura total não é o objetivo primário.
* Heurísticas probabilísticas são preferíveis a inferências agressivas.

### Aplicação Prática
Esta decisão fundamenta regras como:
* Refinamento probabilístico de *zombie detection*.
* Redução de suspeita quando há registo explícito em DI.
* Redução de suspeita quando existe interface correspondente e uso polimórfico.
* Proteção para tipos estruturais esperados.
* Proteção para *bootstrap* e *top-level startup*.

Também fundamenta decisões como:
* `SmellIndex` relativo em vez de absoluto.
* Escopo de deteção explicitamente documentado.
* Distinção entre suspeita estrutural e confirmação.

### Escopo de Deteção Assumido
O sistema deteta de forma explícita sinais como:
* Referências via `new`.
* Uso genérico.
* `typeof(T)`.
* `nameof(T)`.
* Registos explícitos de DI.
* Uso de interfaces.

O sistema não pretende detetar de forma universal:
* *Reflection* dinâmica.
* *Plugin loading*.
* *Assembly scanning*.
* *Code generation runtime*.

### Consequências
**Positivas:**
* Menos falsos positivos.
* Análise mais confiável.
* Heurísticas mais úteis em projetos reais.
* Melhor aceitação prática da ferramenta.

**Negativas:**
* Parte do comportamento dinâmico fica fora do escopo.
* Os resultados precisam de ser interpretados como evidência, não verdade absoluta.

---
