📜 Architectural Decision Records

Este documento consolida as principais decisões arquiteturais do RefactorScope.

O objetivo dos ADRs é:

preservar decisões estruturais importantes

evitar deriva arquitetural

documentar trade-offs técnicos

permitir evolução controlada do sistema

🧱 Core Architecture
ADR-001 — Arquitetura Fundamental do RefactorScope

Status: Accepted

Contexto

O RefactorScope foi criado como uma ferramenta de análise estrutural para apoiar processos de refatoração arquitetural.

Experiências anteriores mostraram que ferramentas de análise frequentemente sofrem de:

acoplamento entre módulos

pipelines implícitos

crescimento descontrolado de escopo

O objetivo do RefactorScope 1.0 é oferecer:

diagnóstico estrutural determinístico

arquitetura modular

pipeline previsível

Decisão

O RefactorScope adota um modelo de:

Pipeline Orquestrado de Analisadores Independentes

Fluxo:

Configuração
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
Princípios

Analyzers são independentes

Analyzers não se comunicam entre si

Comunicação ocorre apenas via Result Objects

Execução determinística

Escopo controlado

Núcleo agnóstico à linguagem

Escopo do 1.0 é estrutural

ADR-002 — Modelo Estrutural Agnóstico

Status: Accepted

Contexto

Analisar diretamente código C# acoplaria o sistema ao parser.

Para permitir evolução futura e suporte a múltiplas linguagens, foi necessário criar um modelo intermediário neutro.

Decisão

O RefactorScope utiliza um Modelo Estrutural Agnóstico.

Fluxo:

Código Fonte
   ↓
Parser
   ↓
Modelo Estrutural
   ↓
Analyzers
Componentes
ArquivoInfo

Representa arquivos analisados.

Contém:

caminho relativo

namespace

tipos declarados

código fonte

TipoInfo

Representa:

classes

interfaces

records

structs

Contém:

nome

namespace

arquivo

referências estruturais

ReferenciaInfo

Representa dependências entre tipos.

Estrutura de Diretórios

Usada para:

análise modular

geração de dumps para IA

ADR-003 — Contrato de Execução dos Analyzers

Status: Accepted

Todos os analyzers seguem o contrato:

public interface IAnalyzer
{
    IAnalysisResult Analyze(AnalysisContext context);
}
Regras

Analyzer:

✔ recebe AnalysisContext
✔ executa isoladamente
✔ retorna IAnalysisResult

Analyzer não pode:

❌ acessar outros resultados
❌ acessar filesystem
❌ alterar contexto
❌ depender de outro analyzer

ADR-004 — Orquestrador e Consolidação

Status: Accepted

O sistema possui um Orquestrador Central.

Responsável por:

selecionar analyzers ativos

executar analyzers

consolidar resultados

O orquestrador não interpreta resultados.

Ele apenas produz:

ConsolidatedReport
ADR-005 — Sistema de Configuração

Status: Accepted

O sistema utiliza configuração externa via:

refactorscope.json

Permite definir:

escopo da análise

analisadores ativos

regras de camada

fitness gates

Exemplo:

{
  "scope": {
    "include": ["src/Core"],
    "exclude": ["tests"]
  },
  "analyzers": {
    "zombie": true,
    "coupling": true
  }
}
🔬 Analysis & Detection
ADR-EXP-011 — Zombie Detection Refinement

Status: Accepted

Problema:

Arquiteturas modernas (DI, Strategy, Factory) geram falsos positivos de código morto.

Decisão

Adotar modelo probabilístico em 3 camadas.

Camada 0 — Suspeita Estrutural
UsageCount == 0

Classificado como:

ZombieSuspect
Camada 1 — Contaminação por DI

Detecta padrões:

AddScoped<
AddTransient<
AddSingleton<

Reduz probabilidade de zombie.

Camada 2 — Polimorfismo

Se:

classe implementa interface

interface é usada

Então probabilidade de zombie é reduzida.

ADR-EXP-007 — Redefinição do Modelo de Zombie

Status: Accepted

Classe só é Confirmed Zombie se:

Sem referência estrutural

Sem absolvição probabilística

Sem DI

Sem uso via interface

Não é tipo estrutural do sistema

Categorias finais:

Structural Candidate

Suspicious

Absolved

Confirmed Zombie

Regra de publicação:

SelfAnalysis → Confirmed Zombies MUST equal 0
ADR-EXP-008 — SmellIndex Relativo

Status: Accepted

Problema:

SmellIndex baseado em números absolutos distorce resultados.

Nova fórmula:

deadRatio = confirmedZombies / totalClasses
legacyRatio = legacyCount / totalClasses
isolationRatio = isolatedCoreCount / totalClasses

SmellIndex =
(deadRatio * 40)
+ (legacyRatio * 20)
+ (isolationRatio * 20)
+ (entropy * 20)

Escala: 0–100

ADR-EXP-012 — Precisão > Cobertura Total

Status: Accepted

O RefactorScope prioriza:

Precisão estrutural > cobertura universal

Isso significa:

menos falsos positivos

aceitação de falsos negativos documentados

ADR-EXP-013 — Escopo de Detecção

Detecta automaticamente:

referências via new

uso genérico

registro DI explícito

interfaces utilizadas

typeof(T)

Não detecta:

reflection dinâmica

plugin loading

assembly scanning

code generation

📊 Data & Visualization
ADR-006 — Exportação de Datasets BI

Status: Accepted

RefactorScope gera datasets para BI.

Arquivos:

dataset_structural_overview.csv
dataset_arch_health.csv
dataset_type_risk.csv
dataset_entrypoints.csv
dataset_coupling_matrix.csv

Esses datasets alimentam:

QuickSight

Power BI

dashboards analíticos

ADR-007 — Observabilidade Arquitetural

Status: Accepted

O sistema passa a registrar histórico.

Arquivo:

datasets/trend/structural_history.csv

Campos:

Timestamp
Scope
StructuralScore
Coupling
ZombieRate
IsolationRate
CoreDensity

Permite:

monitoramento temporal

detecção de regressão arquitetural

ADR-EXP-007 — Dashboard Offline Determinístico

Status: Proposed

Dashboard gerado sem JS runtime.

Estrutura:

refactorscope-output/
   dashboards/
      index.html

Gráficos:

SVG

pré-renderizados

offline friendly

🧪 Testing Strategy
ADR-TEST-001 — Estratégia de Testes

Status: Proposed

O RefactorScope deve possuir um projeto:

RefactorScope.Tests

Ferramentas:

xUnit

FluentAssertions

Testes Mínimos (Quick Win)

Cobrir:

Entropia de Shannon

entropia zero para string uniforme

entropia crescente com diversidade

Pipeline Analyzer

Validar:

execução de analyzers

ordem correta

integridade do relatório

Fitness Gates

Validar:

execução após analyzers

bloqueio de pipeline quando FAIL

ConsolidatedReport

Snapshot simples para validar estrutura do relatório.

🔬 Testes do Parser Textual

Parser textual possui riscos específicos.

Devem existir testes dedicados.

1 — String Trap

Evitar interpretar URLs como namespaces.

Entrada:

string url = "http://example.com";

Resultado esperado:

Nenhum namespace detectado
2 — Comentários de Linha

Código comentado não deve gerar símbolos.

 // using Fake.Namespace
3 — Comentários de Bloco
/*
namespace Fake
class Fake
*/

Não deve gerar tipos.

4 — Strings Multilinha

Strings com código dentro não devem gerar parsing.

var code = "class Fake {}";
5 — Código Parcial

Parser deve tolerar:

arquivos incompletos

código quebrado

syntax errors

6 — Modern C# Tokens

Ignorar:

record
init
with
🧭 Backlog ADRs
ADR-BACKLOG-002 — Snapshot Consistency

Garantir consistência total dos resultados antes de aplicar governança.

Planejado para v1.2.

ADR-BACKLOG-003 — Advisory vs CI Messaging

Separar:

diagnóstico

política de governança

Planejado para v1.2.