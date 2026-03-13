# 🧬 RefactorScope



O **RefactorScope** é uma ferramenta de **análise estrutural estática** projetada para auditar, visualizar e garantir a saúde arquitetural de bases de código C#. 

Em vez de focar apenas em erros de sintaxe ou formatação de código (como os linters tradicionais), o RefactorScope atua como um **scanner de higiene estrutural forense**. Ele mapeia dependências, avalia a aderência aos princípios arquiteturais e emite painéis visuais ricos para guiar refatorações complexas.

---

## 💡 A Origem (Projeto Scriptome)

O RefactorScope nasceu de uma necessidade prática: auditar e guiar a refatoração do **Scriptome** — uma ferramenta complexa de análise narrativa desenvolvida em C#. À medida que o sistema crescia, tornou-se crítico detectar código morto, verificar o isolamento da camada Core, medir o acoplamento e identificar o "desalinhamento" (*drift*) de namespaces. 

O RefactorScope foi então extraído como uma **ferramenta independente e agnóstica**, capaz de analisar qualquer base de código C#.

---

## 🎯 Principais Capacidades

* 🔍 **Auditoria Estrutural Rápida:** Extração de dependências sem a necessidade de compilar o código (independente de APIs pesadas como o Roslyn).
* 🧠 **Detecção Heurística de Código Morto:** Identificação de candidatos a código morto (*Zombies* / *Unresolved*) refinada por regras probabilísticas (Injeção de Dependência, Polimorfismo, etc.).
* 🧱 **Análise de Isolamento e SOLID:** Validação de regras de camadas (Ex: *Core não deve depender de Infrastructure*).
* 🔗 **Acoplamento Implícito:** Mapeamento de tensões arquiteturais, gargalos (*Hubs*) e cálculo da Distância da Sequência Principal (A/I/D).
* 📊 **Exportação Rica:** Geração de *Dashboards* HTML interativos (estilo HUD/Cyberpunk), relatórios em Markdown e datasets para integração com ferramentas de BI e IA.

---

## 📚 Documentação (Knowledge Base)

Para manter este repositório limpo, a documentação detalhada foi dividida por domínios. Consulte os guias na pasta [`docs/`](./docs/) para entender o funcionamento interno do RefactorScope:

| Módulo | Documento | Descrição |
| :--- | :--- | :--- |
| **Visão Geral** | [Arquitetura e Fluxo](./docs/Architecture.md) | O pipeline de execução, motores e a árvore de domínios. |
| **Motor de Extração** | [Parser Design](./docs/Parser-design.md) | Como funcionam as estratégias de extração (Regex, Textual, Híbrido). |
| **Regras e Negócio** | [Metodologia e Métricas](./docs/Methodology.md) | O detalhamento do Refinamento Probabilístico, RDI (Esforço) e *Smell Index*. |
| **Visualização** | [Artefatos de Exportação](./docs/Outputs.md) | A anatomia dos Dashboards HTML e o uso da CLI. |
| **Testes** | [Suíte de Testes](./docs/Tests.md) | A cobertura forense do MVP e cenários validados. |
| **Governança** | [Registros de Decisão (ADRs)](./docs/ADRs.md) | O histórico de decisões arquiteturais do projeto. |
| **Referência Rápida**| [Apêndice Técnico](./docs/Appendix.md) | Resumo consolidado das fórmulas e limites do sistema. |

---

## ⚙️ Configuração (`refactorscope.json`)

Todo o comportamento do RefactorScope é governado pelo arquivo `refactorscope.json`, que deve ser colocado na raiz de execução.

### Exemplo de Configuração

```json
{
  "rootPath": "C:\\Users\\romul\\source\\repos\\Scriptome",
  "outputPath": "refactorscope-output",
  "include": [],
  "exclude": [ "bin", "obj", ".vs", "refactorscope-output" ],
  "parser": "Selective",
  "estimator": { "enabled": true },
  "analyzers": {
    "zombie": true,
    "zombieRefinement": true,
    "architecture": true,
    "coreIsolation": true,
    "entrypoints": true,
    "coupling": true,
    "solid": true,
    "statistics": true
  },
  "zombieDetection": {
    "enableRefinement": true,
    "globalRateThreshold_DI": 0.25,
    "globalRateThreshold_Interface": 0.20,
    "diProbability": 0.15,
    "interfaceProbability": 0.20,
    "minUnresolvedProbabilityThreshold": 0.30
  },
  "layerRules": {
    "UI": { "namespaceContains": [ "Reporting", "CLI" ] },
    "Core": { "namespaceContains": [ "Core", "Analyzers", "Model" ] },
    "Infra": { "namespaceContains": [ "Infrastructure" ] }
  },
  "exporters": [ "htmlDashboard", "dumpAnalysis" ],
  "dashboard": { "theme": "midnight-blue" }
}

```
Detalhamento das Opções

    ⚠️ RECOMENDAÇÃO ARQUITETURAL: É fortemente aconselhado manter os valores da seção zombieDetection e analyzers nos seus padrões de fábrica (default). O sistema foi calibrado exaustivamente para reduzir falsos positivos. Alterar os thresholds probabilísticos pode desestabilizar os resultados e o cálculo de esforço (RDI).

    rootPath / outputPath: Caminhos absolutos ou relativos para o projeto a ser analisado e para a pasta onde os relatórios serão gerados.

    include / exclude: Arrays de strings para filtrar diretórios. (Recomenda-se sempre excluir bin, obj e pastas de testes).

    parser: Define o motor de leitura.

        Opções: "CSharpRegex" (Rápido), "Selective" (Híbrido e Recomendado), "Adaptive" (Recuperação), "Incremental".

    analyzers & estimator: Chaves de ativação (true/false) para ligar/desligar partes da inteligência do sistema. Mantenha todas ativas para a experiência completa.

    zombieDetection: (Legado de nomenclatura para Structural Candidates). Controla os pesos das heurísticas de Injeção de Dependência e Polimorfismo. Mantenha os valores padrão.

    layerRules: Extremamente importante. Aqui você mapeia a sua arquitetura. Use namespaceContains, nameStartsWith ou nameEquals para ensinar o RefactorScope quais pastas/namespaces pertencem a quais camadas (UI, Core, Infra, etc).

    exporters: Quais formatos exportar.

        Opções: "htmlDashboard", "dumpAnalysis" (JSON consolidado), "dumpIA" (JSON otimizado para prompts).

    dashboard.theme: Define a paleta visual dos painéis.

        Opções: "midnight-blue" (Padrão), "ember-ops", "neon-grid".

🚀 Como Usar (Quick Start)

Com o refactorscope.json configurado:
Bash

# Rodando via código fonte
dotnet run --project RefactorScope.CLI

# Ou via ferramenta instalada
RefactorScope analyze

A revisão humana é sempre necessária. O RefactorScope aponta as evidências probabilísticas; o arquiteto toma as decisões de refatoração.