# Resumo Executivo de Testes

## Visão Geral

A suíte de testes do RefactorScope atualmente protege o núcleo analítico do projeto com um nível de confiança maior do que o número global de cobertura, isoladamente, pode sugerir.

- **Cobertura global de linhas:** `46,53%`
- **Linhas cobertas:** `14.812`
- **Total de linhas:** `31.832`

O estado atual está especialmente forte nos domínios que mais importam para o MVP:

- motores de parsing
- arena de comparação de parsers
- pipeline de classificação estrutural
- heurísticas de estimativa
- validação estatística
- snapshots de relatório

---

## Leitura Executiva

O perfil atual de testes mostra um padrão claro:

- os **motores analíticos e matemáticos** agora estão bem protegidos
- a **média global ainda está comprimida** por camadas visuais/exportadoras muito grandes
- as maiores lacunas remanescentes estão concentradas em:
  - geração de dashboards HTML
  - renderizadores visuais
  - renderizadores de mapas de rota
  - classes grandes de composição de saída

Na prática, isso significa que o MVP está mais protegido do que a porcentagem global bruta sugere à primeira vista.

---

## Cobertura por Domínio Raiz

| Domínio | Cobertura | Status |
|---|---:|---|
| RefactorScope.Estimation | 91,26% | 🟢 Forte |
| RefactorScope.Statistics | 78,57% | 🟡 Boa |
| RefactorScope.Execution | 66,22% | 🟡 Boa |
| RefactorScope.Parsers | 62,90% | 🟡 Boa |
| RefactorScope.Core | 48,89% | 🔴 Em expansão |
| RefactorScope.Exporters | 46,55% | 🔴 Em expansão |
| RefactorScope.Analyzers | 33,35% | 🔴 Baixa |
| RefactorScope.CLI | 28,29% | 🔴 Baixa |
| RefactorScope.Infrastructure | 15,90% | 🔴 Baixa |

---

## Áreas Mais Fortes

As áreas mais bem testadas do sistema atualmente são:

- `Execution.Dump`
- `Execution.Dump.Strategies`
- `Estimation.Scoring`
- `Estimation.Classification`
- `Exporters.Dumps`
- `Core.Reporting`
- `Parsers.Common`
- `Core.Parsing.Arena`

Esses módulos são especialmente relevantes porque são determinísticos, fáceis de isolar e diretamente ligados à confiabilidade analítica do MVP.

---

## Principais Puxadores para Baixo da Cobertura

A porcentagem global atual é fortemente impactada por um pequeno conjunto de classes grandes e pesadas:

- `ModuleRouteMapRenderer`
- `SimpleStructureMapRenderer`
- `StructuralInventoryExporter`
- `DashboardMetricsCalculator`
- `QualityDashboardExporter`

Essas classes são grandes e cheias de ramificações, então derrubam a média mais rápido do que módulos analíticos menores conseguem elevá-la.

---

## Principais Hotspots de Risco

| Classe | Método | Complexidade |
|---|---|---:|
| ParserArenaDashboardExporter | GenerateHtml(...) | 108 |
| TableRenderer | Render(...) | 108 |
| FileSize | GetSuffix() | 79 |
| CSharpRegexParser | PrepareStructuralScanSource(...) | 72 |
| TerminalRenderer | ResolveModuleColor(...) | 66 |

Esses hotspots são os candidatos mais claros para:
- futura decomposição
- testes orientados a snapshot
- refatoração pós-MVP

---

## O Que Isso Significa para o MVP

Do ponto de vista prático do MVP, o estado atual dos testes é saudável o suficiente para sustentar o lançamento.

### Por quê

Porque as partes com melhor cobertura são exatamente as que movem o raciocínio do produto:

- extração
- interpretação estrutural
- comparação
- sanidade estatística
- estimativa de dificuldade
- snapshots executivos

### O que ainda está em expansão

As áreas com menor cobertura estão, em sua maioria, ligadas a camadas de apresentação, renderização e publicação.

Isso significa que o principal risco remanescente não está na **lógica analítica central**, mas na **superfície visual/de saída**.

---

> O RefactorScope já atingiu uma cobertura forte em seus módulos analíticos centrais, especialmente parsing, comparação em arena, estimativa e estatística. A cobertura global atual ainda é puxada para baixo por camadas visuais/exportadoras muito grandes, que permanecem como uma frente ativa de expansão pós-MVP.

---