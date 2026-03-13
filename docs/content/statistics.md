# Documentação: Módulo de Estatística (RefactorScope.Statistics)

## Visão Geral

Apesar da nomenclatura, o módulo RefactorScope.Statistics atua primordialmente como um motor observacional e verificador de integridade de payload.

Sua principal responsabilidade no ecossistema do RefactorScope não é realizar inferências estatísticas formais ou análises probabilísticas avançadas, mas sim atestar a sanidade dos dados relatados e mensurar o quão completo está o conjunto de evidências gerado pelos parsers. O módulo opera de forma estritamente não-bloqueante (fail-safe), garantindo que falhas na consolidação de métricas não abortem a execução principal.
O Que o Módulo Realmente Avalia (Coverage vs. Confidence)

O principal indicador derivado deste módulo para os dashboards executivos é o Statistics Coverage (Cobertura Estatística). Este indicador responde à pergunta: "Quanto do payload esperado chegou utilizável para leitura executiva?".

### O cálculo de cobertura avalia:

Disponibilidade do Relatório: Verifica se a estrutura StatisticsReport foi instanciada e devolvida com sucesso.

Presença de Blocos Fundamentais: Confirma a existência dos blocos estruturais de confiança (ParsingConfidence) e o resumo de métricas (MetricsStatisticsSummary).

Preenchimento de Campos-Chave: Valida se métricas primárias contêm valores populados e lógicos, como as médias de classes por arquivo, referências por classe e a proporção de candidatos não resolvidos (unresolved ratio).

Sanidade Matemática Básica: O motor aplica proteções nativas contra anomalias de cálculo, como a prevenção de divisões por zero ao estipular totais de arquivos e tipos.

## Limitações e Alinhamento de Expectativas

Para manter a clareza sobre os limites arquiteturais da ferramenta, é documentado que o módulo atual NÃO mede:

#### Rigor estatístico formal ou significância inferencial dos dados.

#### Tamanho amostral efetivo ou a confiança matemática do modelo de parsing.

#### Consistência metodológica avançada ou estabilidade estrutural cruzada entre múltiplas execuções contínuas.

**Nota:** Exibimos a métrica no dashboard como "Coverage" justamente porque o módulo ainda não expõe um "Evidence Score" formal.

## Arquitetura e Componentes Principais

O fluxo de integridade do payload é dividido em três componentes distintos, separando o motor de cálculo da orquestração e da exibição:

**ValidationEngine:** O motor estático e observacional. Responsável por ler o modelo estrutural consolidado, aplicar as proteções matemáticas e extrair o StatisticsReport de forma segura (RunSafely), delegando logs de erro sem quebrar o pipeline.

**StatisticsValidationAnalyzer:** Atua como o padrão Adapter. Ele envelopa o ValidationEngine para adequá-lo ao contrato IAnalyzer, permitindo que o orquestrador do RefactorScope o invoque de forma padronizada, respeitando regras de opt-in/opt-out de configuração.

**DashboardMetricsCalculator:** Localizado na camada de exportação (Exporters.Projections), atua como a ponte entre o dado bruto e a interface. Ele não gera análises, mas "mastiga" o StatisticsResult para calcular o score executivo de cobertura (CalculateStatisticsCoverageScore) que alimentará os relatórios HTML.