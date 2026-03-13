# Metodologia de Análise e Métricas (Methodology & Metrics)

O RefactorScope executa uma **análise estrutural estática** para avaliar a saúde arquitetural de uma base de código C#. O sistema foca na detecção de **sinais arquiteturais**, não em violações definitivas. O código não é executado em nenhum momento.

A metodologia segue quatro princípios fundamentais:
1. Observação Estrutural
2. Interpretação Heurística
3. Refinamento Probabilístico
4. Verificação Humana

---

## 1. Detecção de Candidatos Estruturais (Antigo "Zombie Detection")

> **Nota de Arquitetura (ADR-EXP-007):** A análise é puramente estrutural. A ausência de referências não declara a "morte" definitiva de uma classe, nem toma decisões de remoção.

O processo inicia mapeando o uso estrutural de todos os tipos. Se `UsageCount == 0`, o tipo é classificado como um **Structural Candidate** (Candidato Estrutural). 

Como arquiteturas modernas utilizam injeção de dependência e polimorfismo, o sistema aplica camadas de **Refinamento Probabilístico** para reduzir falsos positivos:

* **Camada 1 (Injeção de Dependência):** Detecta padrões de registro como `AddScoped<`, `AddTransient<` e `AddSingleton<`.
* **Camada 2 (Uso de Interface):** Se a classe implementa uma interface e essa interface é referenciada no sistema, a probabilidade de ser código morto cai drasticamente.
* **Camada 3 (Regras de Omissão e SOLID):** Avalia sufixos conhecidos (ex: Orquestradores, Handlers). Uma classe pública sem uso direto (`PublicZeroUsageOmissionRule`), mas que atua como orquestradora, é protegida contra marcações indevidas.

Candidatos que sobrevivem a todos os filtros sem explicação arquitetural tornam-se **Unresolved Candidates** e exigem inspeção manual.

---

## 2. Acoplamento e Métricas Complementares

A análise de acoplamento foi expandida para detectar concentrações de dependência que indicam fragilidade ou hubs arquiteturais.

* **Fan-Out:** Número de dependências de saída. Um alto Fan-Out sugere uma classe com alta concentração de responsabilidade (ex: Orquestradores) ou forte acoplamento arquitetural.
* **Fan-In:** Número de dependências de entrada. Um alto Fan-In indica serviços core ou infraestrutura compartilhada crítica para a estabilidade do sistema.
* **Dominância (Dominance):** Mede a influência relativa de um componente na rede. Calculado por `FanOut / (FanOut + FanIn)`. Valores próximos a **1** indicam componentes que exercem forte influência direcional (camadas de coordenação ou integração).

### Acoplamento Implícito
Identifica classes cujas dependências convergem assimetricamente para um módulo específico. Não é necessariamente um erro de design (pode ser um Adapter legítimo), mas serve como alerta de tensão estrutural.

---

## 3. Estabilidade e "Architectural Galaxy"

O sistema avalia a relação de cada módulo com a linha de equilíbrio arquitetural usando as métricas clássicas de Robert C. Martin:

* **Abstração (A):** `A = Na / Nc` (Proporção de tipos abstratos/interfaces em relação ao total).
* **Instabilidade (I):** `I = Ce / (Ce + Ca)` (Proporção de dependências externas vs internas).
* **Distância da Sequência Principal (D):** `D = | A + I − 1 |`. 



Módulos com alto valor de **D** indicam forte tensão: ou são concretos e rígidos demais (difíceis de alterar), ou abstratos e instáveis demais (inúteis).

---

## 4. Estimativa de Esforço (Refactor Difficulty Index - RDI)

*Esta seção detalha o motor de estimativa integrado ao pipeline de análise.*

O RefactorScope não apenas aponta os problemas, mas calcula o esforço estimado para resolvê-los usando o **RDI (Refactor Difficulty Index)**. O cálculo é baseado em pressões estruturais e modelagem de risco:

* **Pressão de Arquivo:** Avalia a proporção de classes por arquivo. (Muitas classes em um único arquivo aumentam o atrito de refatoração).
* **Structural Risk Model:** Calcula o nível de degradação ponderando duas variáveis principais:
  * `NamespaceDriftRatio`: Proporção de tipos com desalinhamento entre diretório físico e namespace lógico.
  * `UnresolvedCandidateRatio`: Proporção de "zombies" confirmados após o refinamento probabilístico.

O risco estrutural é normalizado e combinado com a complexidade geral para gerar uma **Estimativa de Horas** e um nível de **Dificuldade** (Low, Medium, High, Critical).

---

## 5. Higiene do Código e Fitness Gates

A saúde global do projeto é consolidada no **Smell Index**, um índice normalizado (0-100) que determina o status de higiene:

`SmellIndex = (DeadRatio * 40) + (LegacyRatio * 20) + (IsolationRatio * 20) + (Entropy * 20)`

* **Entropia:** Baseada na Entropia de Shannon, mede a variabilidade léxica do código como indicativo de desordem estrutural.
* **Status:** Varia de *Healthy* (0-20) até *Structural Risk* (80-100).