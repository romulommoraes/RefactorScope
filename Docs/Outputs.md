## Artefatos de Exportação (Output Generation)

O módulo de Export é responsável por traduzir o modelo estrutural agnóstico em artefatos de alto valor para tomada de decisão. Ele gera dois formatos principais: Dashboards Interativos (HTML) para exploração visual profunda e Relatórios Textuais (Markdown) para auditoria rápida, revisões de PR (Pull Requests) e documentação estática.
## 📊 Dashboards Interativos (HTML)

Os painéis HTML são gerados de forma estática (não exigem servidor web) e utilizam tecnologias de renderização cliente para entregar uma experiência fluida. Todos suportam alternância de temas dinâmicos (ex: Midnight Blue, Ember Ops, Neon Grid).

    index.html (RefactorScope Hub)
    Atua como o ponto de entrada principal (Landing Page). Ele consolida a navegação e fornece um resumo de alto nível que redireciona o usuário para os painéis especializados.

    ArchitecturalDashboard.html
    O painel de saúde sistêmica. Focado nas macro-métricas da arquitetura, exibe a relação de dependências entre módulos, análise de instabilidade, nível de abstração e as tensões arquiteturais do projeto.

    StructuralDashboard.html
    O painel forense. Focado na estrutura interna do código, exibe a hierarquia de tipos, alinhamento entre diretórios físicos e namespaces lógicos (Drift Logico/Arquitetural), e mapeia visualmente os candidatos a código morto (Zombies).

    QualityDashboard.html
    O painel de validação de regras (Gates). Mostra o status de aprovação ou reprovação do projeto em relação às regras de qualidade configuradas (Fitness Gates), como isolamento do Core, limites de acoplamento e dependências proibidas.

    ParsingDashboard.html
    O painel de telemetria do motor. Exibe métricas de execução técnica: tempo de processamento, taxa de sucesso na extração estrutural, volume de arquivos classificados como Safe vs Complex e possíveis alertas de plausibilidade do parser.

    Painéis de Comparação (Arena Mode)

        ParserArenaDashboard.html, ParserComparativeDashboard.html e ParserComparativeSelfDashboard.html
        Gerados especificamente quando o RefactorScope roda em modo BatchArena ou Comparative. Eles não analisam o código do projeto em si, mas sim a performance dos próprios motores de Parsing. Mostram comparativos de tempo, alocação e cobertura entre o motor Regex, Textual e Híbrido, ajudando a escolher a melhor estratégia para o repositório.

📄 Relatórios Textuais (Markdown)

Arquivos enxutos e diretos, otimizados para leitura humana rápida, publicações de Release Notes e integração com sistemas de CI/CD.

    Relatorio_Executivo.md
    O resumo gerencial da rodada de análise.

        Traz a interpretação operacional final (Overall Readiness).

        Consolida os sinais primários: quantos candidatos a código morto foram encontrados, quantos foram explicados por similaridade de padrão (DI, Interfaces) e quantos precisam de inspeção manual rigorosa.

        Informa a banda de confiança do parser (ex: High Confidence).

    Relatorio_Arquitetural.md
    O aprofundamento técnico focado em métricas formais de design de software.

        Estrutura de Pastas: Gera a árvore limpa do escopo analisado.

        Métricas Clássicas: Explica e calcula a Abstração (A), Instabilidade (I) e Distância da Sequência Principal (D) de cada módulo, baseadas na teoria de Robert C. Martin.

        Acoplamento Implícito: Lista as classes que apresentam uma anomalia heurística de forte concentração de dependências direcionais (alto fan-out para módulos específicos), servindo como um alerta para refatorações como extração de adaptadores ou orquestradores.



        🛠️ Tecnologias Utilizadas (Tech Stack)

O RefactorScope foi projetado para ser uma ferramenta leve, autossuficiente e visualmente rica. A arquitetura tecnológica é dividida em três pilares principais: o motor de análise, o frontend estático (Dashboards) e a interface de terminal.
Motor de Análise (Backend Core)

    C# / .NET 8+: Linguagem base do projeto. Todo o parsing, classificação estrutural e cálculo de métricas (Instabilidade, Abstração, Esforço) é feito de forma nativa.

    Expressões Regulares Avançadas (Regex): Utilizadas intensivamente nos motores de parsing rápidos e em heurísticas de fallback, garantindo alta performance sem depender do Roslyn (compilador).

Dashboards e Visualização (Frontend Exports)

Os painéis HTML gerados pelo módulo Export não exigem bibliotecas pesadas de SPA (como React ou Angular). Eles utilizam uma stack visual leve focada em estética Cyberpunk / Sci-Fi e alta performance de renderização:

    p5.js: Biblioteca focada em creative coding baseada em Canvas. Utilizada para renderizar visualizações complexas e dinâmicas que o CSS tradicional não suporta (como grafos de dependência e plotagem estrutural livre).

    Charts.css: Framework de visualização de dados em puro CSS. Garante tabelas estruturadas e gráficos (barras, linhas) ultra-leves e responsivos, sem o peso de bibliotecas de gráficos em JavaScript.

    Augmented UI: Biblioteca CSS especializada em bordas futuristas e layouts "chanfrados". É a responsável pelo visual tático/militar (painéis recortados, miras óticas e cantos angulados) presente em todos os dashboards.

💻 Interface de Linha de Comando (CLI)

O RefactorScope não é apenas uma ferramenta de background; ele fornece um feedback rico e em tempo real no terminal durante o processamento. Para quebrar a limitação visual dos consoles tradicionais, o módulo CLI utiliza uma biblioteca especializada:
🎨 Spectre.Console

Toda a renderização do terminal — desde o cabeçalho inicial até as tabelas de estimativa de esforço — é orquestrada pela biblioteca Spectre.Console.

Através da nossa classe de infraestrutura estática TerminalRenderer, encapsulamos o poder do Spectre para criar uma identidade visual única (Neon Matrix / Hacker aesthetics), utilizando:

    Rich Markup: Cores hexadecimais absolutas (como verde neon #00ff00, hotpink e deepskyblue1) para destacar métricas de aviso, sucesso e erros críticos.

    Componentes Avançados: Renderização de Panels (caixas com bordas duplas), Tables (estruturas tabulares dinâmicas para listar os módulos e pontuações de saúde) e Rules (linhas divisórias centralizadas).

    Spinners Asíncronos: Indicadores de progresso visuais (Status().Spinner()) que mantêm o usuário informado durante etapas de parsing pesado sem travar o console.

    Barras de Progresso Inline: A CLI constrói barras de esforço heurístico (█░░░) diretamente no log do terminal, facilitando a interpretação imediata sem precisar abrir o relatório final.