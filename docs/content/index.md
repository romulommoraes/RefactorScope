# RefactorScope

Tenho a impressão de que quase todo desenvolvedor já passou por isso em alguma refatoração: uma mudança puxa outra, começam os erros em cascata, surgem warnings por todo lado, o projeto para de compilar com confiança… e em algum momento você já não sabe mais o que é legado, o que ainda é usado, onde o acoplamento começou a apertar e até onde os namespaces deixaram de fazer sentido.

Eu estava exatamente nesse ponto com um projeto pessoal.

A refatoração estava ficando tão dolorosa que cheguei a considerar o caminho mais tentador de todos: abandonar a base atual e começar do zero, com uma arquitetura nova.

Mas aí veio a pergunta que mudou tudo:

**e se, em vez de recomeçar no escuro, eu tivesse uma ferramenta para me mostrar onde estava a tensão arquitetural?**

Foi daí que nasceu o **RefactorScope**.

Nos últimos **14 dias**, do primeiro commit até hoje, eu parei o que estava fazendo para construir uma ferramenta de **análise estrutural estática** voltada para auditoria e refatoração de projetos C#. A ideia era simples no começo, mas o escopo cresceu bastante no processo — e eu cresci junto como desenvolvedor, engenheiro e arquiteto de software.

Trabalhei sozinho, mas com forte apoio de IA no ciclo de desenvolvimento — principalmente **ChatGPT** e **Gemini**, muitas vezes auditando o código e as minhas decisões. No fim, isso virou menos “usar IA para gerar código” e mais **usar IA como fricção crítica para pensar melhor**.

O resultado foi uma ferramenta que tenta responder perguntas que, em refatorações grandes, costumam ser dolorosas demais de responder manualmente:

- onde existe acoplamento implícito
- onde o legado se mistura com código atual
- onde há candidatos reais a código morto
- onde existe drift estrutural entre pastas, módulos e namespaces
- quão saudável está a arquitetura de forma mais visual e auditável

O RefactorScope faz isso com um pipeline de parsing estrutural leve, sem depender de compilação completa, usando estratégias diferentes de extração e refinamento heurístico para gerar:

- dashboards HTML
- relatórios em Markdown
- datasets analíticos
- artefatos de apoio para inspeção e tomada de decisão

Entre as partes que mais me deram orgulho nesse projeto estão:

- o **parser híbrido**, que tenta equilibrar velocidade e resiliência
- a **camada heurística** para reduzir falsos positivos na detecção de código morto
- os **dashboards visuais no estilo HUD**, que transformaram análise estrutural em algo muito mais legível
- a própria **separação arquitetural da ferramenta**, que nasceu de uma dor real e acabou virando um produto agnóstico

Ele nasceu para me ajudar a refatorar um projeto específico.

Mas no caminho acabou se tornando algo maior: uma ferramenta independente para inspecionar a saúde arquitetural de qualquer base C#.

Foi intenso, cansativo, divertido e muito formativo.

Hoje estou fazendo o lançamento do **MVP**.

O GitHub, a documentação e os detalhes técnicos fazem parte desse lançamento — e, sinceramente, estou feliz não só pelo que construí, mas pelo quanto esse processo me empurrou para frente.