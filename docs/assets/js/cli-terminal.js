const CLITerminal = {
    getHTML: () => {
        // Todas as linhas do terminal organizadas. 
        // d = delay | t = text | c = classes extras
        const lines = [
            { d: 0.1, t: "?? RefactorScope Analysis Scope" },
            { d: 0.2, t: "------------------------------" },
            { d: 0.3, t: "1) Normal Analysis" },
            { d: 0.4, t: "2) Self Analysis (RefactorScope analyzing itself)" },
            { d: 0.7, t: "Select scope (1 or 2) [default=1]: 1" },
            { d: 0.9, t: "?? Normal Analysis Scope Enabled", c: "cli-dim" },
            { d: 1.1, t: " " },
            { d: 1.2, t: "?? Execution Mode" },
            { d: 1.3, t: "------------------------------" },
            { d: 1.4, t: "1) Single Parser | 2) Comparative | 3) Batch Arena" },
            { d: 1.6, t: "Select mode [default=1]: 1" },
            { d: 1.7, t: "?? Single Parser Mode Enabled", c: "cli-dim" },
            { d: 1.9, t: " " },
            { d: 2.0, t: "?? Parser Selection" },
            { d: 2.1, t: "1) Fast (Regex) | 2) Accurate (Selective) | 3) Adaptive" },
            { d: 2.3, t: "Select parser [1-3] (default 2): 2" },
            { d: 2.6, t: " " },
            { d: 2.8, t: "╔════════════════════════════════════════════════════════════════════╗", c: "cli-cyan" },
            { d: 2.9, t: "║ <span class=\"cli-green\">SYSTEM ONLINE</span>                                                      ║", c: "cli-cyan" },
            { d: 3.0, t: "║ <span class=\"cli-pink cli-bold\">REFACTORSCOPE v1.0</span>                                                 ║", c: "cli-cyan" },
            { d: 3.1, t: "║ <span class=\"cli-green cli-bold\">ARCHITECTURAL FORENSICS</span>                                            ║", c: "cli-cyan" },
            { d: 3.2, t: "║ TARGET: <span class=\"cli-yellow\">C:\\Users\\romul\\source\\repos\\Scriptome</span>                      ║", c: "cli-cyan" },
            { d: 3.3, t: "╚════════════════════════════════════════════════════════════════════╝", c: "cli-cyan" },
            { d: 3.5, t: " " },
            { d: 3.7, t: "<span class=\"cli-blue\">></span> Scope: Normal | Mode: SingleParser | Parser: Selective" },
            { d: 3.9, t: "[Selective] Executando baseline global via Regex..." },
            { d: 4.2, t: "<span class=\"cli-green\">/</span> Parsing código..." },
            { d: 4.5, t: "     [Selective] Scanning project for class complexity..." },
            { d: 4.8, t: "[Selective] Safe files: 95 | Complex files: 79" },
            { d: 5.2, t: "Parser: <span class=\"cli-cyan cli-bold\">Hybrid Selective (Accurate Scan)</span>" },
            { d: 5.4, t: " " },
            { d: 5.5, t: "┌───────────────────────────────────────────────────┬─────────────┐" },
            { d: 5.6, t: "│ <span class=\"cli-pink\">Metric</span>                                            │ <span class=\"cli-green\">Value</span>       │" },
            { d: 5.7, t: "├───────────────────────────────────────────────────┼─────────────┤" },
            { d: 5.8, t: "│ Files / Types / References                        │ <span class=\"cli-bold\">189/190/696</span> │" },
            { d: 5.9, t: "│ Execution                                         │ <span class=\"cli-yellow cli-bold\">860 ms</span>      │" },
            { d: 6.0, t: "└───────────────────────────────────────────────────┴─────────────┘" },
            { d: 6.3, t: "<span class=\"cli-green\">?</span> Parsing concluído | [SOLID] Coupling: True | <span class=\"cli-green\">?</span> Análise concluída" },
            { d: 6.5, t: " " },
            { d: 6.8, t: "Architectural Health", c: "cli-divider" },
            { d: 7.0, t: "┌───────────────────┬─────────┬──────────────┬────────────┐" },
            { d: 7.1, t: "│ <span class=\"cli-pink\">Module</span>            │ <span class=\"cli-yellow\">Score</span>   │ <span class=\"cli-yellow\">Unresolved</span>   │ <span class=\"cli-cyan\">Coupling</span>   │" },
            { d: 7.2, t: "├───────────────────┼─────────┼──────────────┼────────────┤" },
            { d: 7.3, t: "│ <span class=\"cli-green\">Analyzers</span>         │ <span class=\"cli-red\">54,0</span>    │ <span class=\"cli-green\">0 (0%)</span>       │ 3,60       │" },
            { d: 7.4, t: "│ <span class=\"cli-yellow\">CLI</span>               │ <span class=\"cli-green\">100,0</span>   │ <span class=\"cli-green\">0 (0%)</span>       │ 2,75       │" },
            { d: 7.5, t: "│ <span class=\"cli-blue\">Core</span>              │ <span class=\"cli-green\">100,0</span>   │ <span class=\"cli-green\">0 (0%)</span>       │ 0,11       │" },
            { d: 7.6, t: "└───────────────────┴─────────┴──────────────┴────────────┘" },
            { d: 8.0, t: "i Unresolved items require manual review.", c: "cli-dim" },
            { d: 8.5, t: "> _", c: "blink-cursor", extraStyle: "margin-top: 12px;" }
        ];

        // O segredo está no join(''): ele junta tudo colado sem quebras de linha fantasmas no HTML
        const linesHTML = lines.map(l => {
            const cssClass = l.c ? `term-line ${l.c}` : `term-line`;
            const style = l.extraStyle ? `--delay: ${l.d}s; ${l.extraStyle}` : `--delay: ${l.d}s;`;
            return `<div class="${cssClass}" style="${style}">${l.t}</div>`;
        }).join('');

        return `
            <div class="cyber-terminal">
                <div class="terminal-header">
                    <span class="dot red"></span><span class="dot yellow"></span><span class="dot green"></span>
                    <span class="title">refactorscope-engine.exe</span>
                </div>
                <div class="terminal-body" id="cli-body-scroll">${linesHTML}</div>
            </div>`;
    },
    
    init: () => {
        const anchor = document.getElementById('cli-terminal-anchor');
        if (!anchor) return;

        anchor.innerHTML = CLITerminal.getHTML();
        const termBody = document.getElementById('cli-body-scroll');
        const termContainer = anchor.querySelector('.cyber-terminal');

        // Garante que o scroll inicie no topo exato do painel
        termBody.scrollTop = 0;

        setTimeout(() => {
            termContainer.classList.add('is-playing');
            
            // Controle de scroll inteligente: só desce se a linha estourar a tela
            let scrollInterval = setInterval(() => {
                if(termBody) {
                    const isAtBottom = termBody.scrollHeight - termBody.clientHeight <= termBody.scrollTop + 50;
                    if (isAtBottom) {
                        termBody.scrollTop = termBody.scrollHeight;
                    }
                }
            }, 50);

            // Tempo total da animação = 8.5s. Desligamos o scroll forçado nos 10s.
            setTimeout(() => clearInterval(scrollInterval), 10000);
        }, 300);
    }
};