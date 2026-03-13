# Arquitetura e Fluxo de Execução

## 1. O Pipeline de Execução do RefactorScope

<div class="chart-embedded" id="p5-pipeline-container">
    <h3>Information Flow Pipeline</h3>
</div>

Este diagrama ilustra o ciclo de vida completo de uma rodada de análise, desde a leitura do código até a geração de valor para o usuário. O fluxo é dividido em quatro estágios isolados:

* **Configuração (Input):** Tudo começa no `refactorscope.json` e nos parâmetros da CLI, que definem as regras do jogo (quais pastas ignorar, quais limites de qualidade aplicar).
* **Parsing (Extração):** O coração da performance. O código bruto passa pelos motores de parsing (Regex, Textual ou Híbrido). O objetivo aqui não é compilar, mas extrair um **Modelo Estrutural** agnóstico e limpo (tipos, namespaces e referências).
* **Análise (Inteligência):** Com o modelo em mãos, os *Analyzers* entram em ação. Eles rodam em paralelo para validar isolamento de camadas, calcular métricas (A/I/D), detectar acoplamento implícito e aplicar as heurísticas de probabilidade para caçar código morto. O resultado dessa fase é a emissão do *RDI* (Estimativa de Esforço).
* **Exportação (Output):** Os dados processados são traduzidos em artefatos de alto valor. O sistema gera os Dashboards Interativos (HTML) para exploração visual, relatórios Markdown para auditoria rápida de PRs, e *dumps* em JSON preparados para alimentar ferramentas de BI ou revisão por IAs.

---

## 2. Árvore Arquitetural e de Domínios

<div class="chart-embedded" id="p5-radial-tree-container">
    <h3>RefactorScope Domain Map</h3>
</div>

Esta visão detalha a taxonomia física e lógica do projeto. O RefactorScope foi desenhado com uma separação rigorosa de responsabilidades, garantindo que o motor de análise não dependa das interfaces de saída.

* **Core & Model:** O centro do sistema. Contém as abstrações principais, o modelo estrutural de dados e as definições de contexto que fluem por toda a aplicação. Nenhuma regra de negócio específica de um parser reside aqui.
* **Parsers & Analyzers:** Onde a extração e a validação acontecem. Diretórios modulares indicam que novos motores de leitura ou novas regras do SOLID podem ser plugados sem alterar o restante da base.
* **Estimation & Metrics:** Módulos dedicados à heurística avançada. Isolam a complexidade matemática do cálculo do *Refactor Difficulty Index (RDI)* e da estabilidade arquitetural de Robert C. Martin.
* **Exporters & CLI:** A borda do sistema. A CLI gerencia a interação com o usuário via terminal (Spectre.Console), enquanto os *Exporters* lidam puramente com formatação, seja gerando CSS estilizado para os dashboards ou serializando dados para análises externas.

---
