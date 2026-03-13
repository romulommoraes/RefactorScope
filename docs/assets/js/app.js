document.addEventListener("DOMContentLoaded", () => {
    const contentDiv = document.getElementById("markdown-container");
    const titleEl = document.getElementById("doc-title");
    const kickerEl = document.getElementById("doc-kicker");
    const statusEl = document.getElementById("doc-status");
    const sidebarMenu = document.getElementById("sidebar-menu");
    
    const langBtn = document.getElementById("langToggleBtn");
    const cyclerBtn = document.getElementById("themeCyclerBtn");
    const themeLink = document.getElementById("dashboard-theme-link");

    // ==========================================
    // ESTADO E IDIOMA
    // ==========================================
    let currentLang = localStorage.getItem("refactorscope-lang") || "pt";

    function applyLanguageUI() {
        if(langBtn) langBtn.setAttribute("data-lang", currentLang);
    }

    if(langBtn) {
        langBtn.addEventListener("click", () => {
            currentLang = currentLang === "pt" ? "en" : "pt";
            localStorage.setItem("refactorscope-lang", currentLang);
            applyLanguageUI();
            
            let hash = window.location.hash.substring(1) || DEFAULT_ROUTE;
            let baseHash = hash.replace("-en", "");
            
            if (currentLang === "en") {
                window.location.hash = baseHash + "-en";
            } else {
                window.location.hash = baseHash;
            }
        });
    }

    // ==========================================
    // GESTÃO DE TEMA
    // ==========================================
    const themeSequence = [
        { id: 'midnight-blue', css: 'assets/css/dashboard-theme-midnight-blue.css' },
        { id: 'ember-ops', css: 'assets/css/dashboard-theme-ember-ops.css' },
        { id: 'neon-grid', css: 'assets/css/dashboard-theme-neon-grid.css' },
        { id: 'yellow-kiddo', css: 'assets/css/dashboard-theme-yellow-kiddo.css' }
    ];

    let currentThemeIndex = 0;
    const savedTheme = localStorage.getItem('refactorscope-dashboard-theme');
    if (savedTheme) {
        const idx = themeSequence.findIndex(t => t.id === savedTheme);
        if (idx !== -1) currentThemeIndex = idx;
    }

    if(cyclerBtn) {
        cyclerBtn.addEventListener("click", () => {
            currentThemeIndex = (currentThemeIndex + 1) % themeSequence.length;
            const t = themeSequence[currentThemeIndex];
            
            themeLink.setAttribute('href', t.css);
            document.documentElement.setAttribute('data-dashboard-theme', t.id);
            localStorage.setItem('refactorscope-dashboard-theme', t.id);

            document.body.style.display = 'none';
            void document.body.offsetHeight; 
            document.body.style.display = '';
        });
    }

    // ==========================================
    // MENU E ROTEAMENTO
    // ==========================================
    function buildMenu() {
        sidebarMenu.innerHTML = "";
        
        const activeKeys = Object.keys(DOCS_MAP).filter(key => {
            return currentLang === "pt" ? !key.endsWith("-en") : key.endsWith("-en");
        });

        activeKeys.forEach(key => {
            const data = DOCS_MAP[key];
            const link = document.createElement("a");
            
            link.href = `#${key}`;
            link.className = "nav-btn"; 
            link.innerHTML = `> ${data.title.toUpperCase()}`;
            link.dataset.route = key;
            
            sidebarMenu.appendChild(link);
        });
    }

    let activeSketches = [];

    async function loadContent() {
        let hash = window.location.hash.substring(1); 
        
        if (!hash || !DOCS_MAP[hash]) {
            window.location.hash = currentLang === "pt" ? "index" : "index-en";
            return; 
        }

        if (hash.endsWith("-en") && currentLang === "pt") {
            currentLang = "en";
            localStorage.setItem("refactorscope-lang", "en");
            applyLanguageUI();
            buildMenu();
        } else if (!hash.endsWith("-en") && currentLang === "en") {
            currentLang = "pt";
            localStorage.setItem("refactorscope-lang", "pt");
            applyLanguageUI();
            buildMenu();
        }

        const routeData = DOCS_MAP[hash];

        titleEl.textContent = routeData.title;
        kickerEl.textContent = routeData.kicker;
        statusEl.textContent = "FETCHING...";
        statusEl.className = "status-indicator fetching";

        document.querySelectorAll("#sidebar-menu a").forEach(btn => {
            if (btn.dataset.route === hash) {
                btn.classList.add("active");
            } else {
                btn.classList.remove("active");
            }
        });

        if (activeSketches.length > 0) {
            activeSketches.forEach(sketch => sketch.remove());
            activeSketches = [];
        }

        try {
            const response = await fetch(routeData.file);
            if (!response.ok) throw new Error("HTTP_404");
            
            const markdownText = await response.text();
            contentDiv.innerHTML = marked.parse(markdownText);
            
            document.querySelectorAll('pre code').forEach((block) => {
                hljs.highlightElement(block);
            });

// Aplica Syntax Highlight
            document.querySelectorAll('pre code').forEach((block) => {
                hljs.highlightElement(block);
            });

            // ==========================================
            // NOVO: SENSOR DE VISÃO PARA O TERMINAL
            // ==========================================
            const terminals = document.querySelectorAll('.cyber-terminal');
            if (terminals.length > 0) {
                const observer = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            // Se viu, dá o play na animação
                            entry.target.classList.add('is-playing');
                        } else {
                            // Se saiu da tela, reseta a animação para tocar de novo na volta
                            entry.target.classList.remove('is-playing');
                        }
                    });
                }, { threshold: 0.2 }); // Dispara quando 20% do terminal aparece na tela
                
                terminals.forEach(term => observer.observe(term));
            }
            // INJEÇÃO DO TERMINAL CLI (Chamada do Módulo Externo)
            if (document.getElementById('cli-terminal-anchor')) {
                CLITerminal.init();
}

            // INJEÇÃO P5 (Rastreada pela memória)
            if (document.getElementById('p5-radial-tree-container') && typeof radialTreeSketch !== 'undefined') activeSketches.push(new p5(radialTreeSketch));
            if (document.getElementById('p5-radial-tree-container') && typeof radialTreeSketch !== 'undefined') activeSketches.push(new p5(radialTreeSketch));
            if (document.getElementById('p5-pipeline-container') && typeof pipelineSketch !== 'undefined') activeSketches.push(new p5(pipelineSketch));

            if (document.getElementById('p5-global-coverage') && typeof globalCoverageSketch !== 'undefined') activeSketches.push(new p5(globalCoverageSketch));
            if (document.getElementById('p5-sector-status') && typeof sectorStatusSketch !== 'undefined') activeSketches.push(new p5(sectorStatusSketch));
            if (document.getElementById('p5-risk-matrix') && typeof riskMatrixSketch !== 'undefined') activeSketches.push(new p5(riskMatrixSketch));
            if (document.getElementById('p5-polarity') && typeof polaritySketch !== 'undefined') activeSketches.push(new p5(polaritySketch));
            if (document.getElementById('p5-critical-mass') && typeof criticalMassSketch !== 'undefined') activeSketches.push(new p5(criticalMassSketch));
            if (document.getElementById('p5-telemetry-hub') && typeof telemetryHubSketch !== 'undefined') activeSketches.push(new p5(telemetryHubSketch));

            statusEl.textContent = "ONLINE";
            statusEl.className = "status-indicator online";
            document.querySelector('.content-wrapper').scrollTo(0, 0);

        } catch (error) {
            console.error("Dashboard Logic Error:", error);
            
            if (error.message === "HTTP_404") {
                contentDiv.innerHTML = `
                    <div style="padding: 40px; text-align: center; background: rgba(0,0,0,0.2); border: 1px solid var(--docs-alert); margin-top: 20px; border-radius: 8px;">
                        <h2 style="margin-top:0; font-family: monospace; color: var(--docs-alert);">[ SYSTEM ERROR 404 ]</h2>
                        <p>Falha ao carregar arquivo físico: <b>${routeData.file}</b></p>
                    </div>
                `;
            } else {
                contentDiv.innerHTML = `
                    <div style="padding: 40px; text-align: center; background: rgba(0,0,0,0.2); border: 1px solid var(--docs-warn); margin-top: 20px; border-radius: 8px;">
                        <h2 style="margin-top:0; font-family: monospace; color: var(--docs-warn);">[ RENDER ENGINE CRASH ]</h2>
                        <p>O arquivo Markdown foi lido, mas um script JS capotou na renderização.</p>
                        <p style="font-size:11px; color:var(--docs-muted);">${error.message}</p>
                    </div>
                `;
            }
            
            statusEl.textContent = "OFFLINE";
            statusEl.className = "status-indicator offline";
        }
    }

    applyLanguageUI();
    buildMenu();
    window.addEventListener("hashchange", loadContent);
    loadContent();
});