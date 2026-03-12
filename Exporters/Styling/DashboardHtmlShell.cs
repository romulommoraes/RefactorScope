using System;
using System.Text;

namespace RefactorScope.Exporters.Styling
{
    /// <summary>
    /// Responsável por renderizar o shell HTML compartilhado
    /// entre os dashboards da suíte visual.
    ///
    /// Objetivo
    /// --------
    /// Remover repetição de:
    /// - doctype
    /// - head
    /// - meta tags
    /// - imports CSS
    /// - abertura e fechamento estrutural do body
    /// - Lógica global de persistência do Optic Mode (Temas)
    /// - [NOVO] Barra de navegação tática cross-dashboard
    /// </summary>
    public static class DashboardHtmlShell
    {
        /// <summary>
        /// Nova assinatura principal.
        /// </summary>
        public static string RenderDocumentStart(string title)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8' />");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0' />");
            sb.AppendLine($"<title>{Html(title)}</title>");

            sb.AppendLine("<script src='assets/vendor/p5.js'></script>");
            sb.AppendLine("<link rel='stylesheet' href='assets/vendor/Charts.css' />");
            sb.AppendLine("<link rel='stylesheet' href='assets/vendor/augmented-ui.css' />");

            sb.AppendLine("<link rel='stylesheet' href='assets/css/dashboard-base.css' />");
            sb.AppendLine("<link rel='stylesheet' href='assets/css/dashboard-components.css' />");
            sb.AppendLine("<link id='dashboard-theme-link' rel='stylesheet' href='assets/css/dashboard-theme.css' />");

            // Injeção Global do CSS do Botão Tático e da Barra de Navegação (Disponível para qualquer Dashboard usar)
            sb.AppendLine("""
<style>
/* Optic Mode Controls */
.optic-mode-wrapper {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 12px;
    width: 100%;
    margin-bottom: 6px;
}

.optic-label {
    font-size: 11px;
    letter-spacing: 0.15em;
    color: #8fa8ff;
    font-weight: 700;
}

.red-tactical-btn {
    width: 50px;
    height: 24px;
    border-radius: 4px;
    background: linear-gradient(180deg, #ff3333 0%, #aa0000 100%);
    border: 1px solid #440000;
    border-top: 1px solid #ff8888;
    border-bottom: 2px solid #550000;
    box-shadow: 
        0 4px 6px rgba(0,0,0,0.8), 
        0 0 12px rgba(255, 0, 0, 0.5),
        inset 0 2px 4px rgba(255,255,255,0.3);
    cursor: pointer;
    position: relative;
    transition: all 0.1s cubic-bezier(0.4, 0.0, 0.2, 1);
    outline: none;
}

.red-tactical-btn::before {
    content: '';
    position: absolute;
    top: 2px; left: 4px; right: 4px; height: 35%;
    background: linear-gradient(180deg, rgba(255,255,255,0.4) 0%, rgba(255,255,255,0) 100%);
    border-radius: 2px;
    pointer-events: none;
}

.red-tactical-btn::after {
    content: '|||';
    position: absolute;
    top: 50%; left: 50%;
    transform: translate(-50%, -50%);
    color: rgba(0,0,0,0.4);
    font-size: 10px;
    font-weight: 900;
    letter-spacing: 2px;
    pointer-events: none;
    text-shadow: 1px 1px 0px rgba(255,255,255,0.2);
}

.red-tactical-btn:active {
    transform: translateY(3px);
    border-top: 1px solid #aa0000;
    border-bottom: 1px solid #220000;
    box-shadow: 
        0 1px 2px rgba(0,0,0,0.9),
        0 0 8px rgba(255, 0, 0, 0.8),
        inset 0 4px 8px rgba(0,0,0,0.5);
}

/* Tactical Navigation Bar */
.tactical-nav-wrapper {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: 16px;
    width: 100%;
    flex-wrap: wrap;
}

.nav-divider {
    width: 2px;
    height: 18px;
    background: rgba(255,255,255,0.1);
    margin: 0 4px;
}

.nav-btn {
    display: inline-block;
    padding: 6px 14px;
    background: rgba(15, 22, 38, 0.6);
    border: 1px solid rgba(0, 229, 255, 0.2);
    color: rgba(0, 229, 255, 0.5);
    font-size: 10px;
    font-family: monospace;
    font-weight: bold;
    letter-spacing: 0.1em;
    text-decoration: none;
    transition: all 0.2s ease;
    cursor: pointer;
}

.nav-btn:hover {
    background: rgba(0, 229, 255, 0.1);
    color: #00e5ff;
    border-color: #00e5ff;
    box-shadow: 0 0 10px rgba(0, 229, 255, 0.2);
}

/* O Módulo Atual fica aceso/ligado */
.nav-btn.active {
    background: rgba(0, 229, 255, 0.15);
    color: #fff;
    border-color: #00e5ff;
    box-shadow: inset 0 0 8px rgba(0, 229, 255, 0.3), 0 0 12px rgba(0, 229, 255, 0.4);
    text-shadow: 0 0 4px rgba(255,255,255,0.5);
    pointer-events: none; /* Desativa o clique na página atual */
}

/* O botão de voltar para o Hub tem cor diferente (Laranja/Alerta) */
.nav-btn.btn-hub {
    border-color: rgba(255, 154, 60, 0.4);
    color: rgba(255, 154, 60, 0.8);
}
.nav-btn.btn-hub:hover {
    background: rgba(255, 154, 60, 0.1);
    color: #ff9a3c;
    border-color: #ff9a3c;
    box-shadow: 0 0 10px rgba(255, 154, 60, 0.3);
}
</style>
""");

            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Interceptador Global: Aplica o tema salvo antes da tela desenhar para não piscar a cor errada.
            sb.AppendLine("""
<script>
(function() {
    const STORAGE_KEY = 'refactorscope-dashboard-theme';
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        const themeLink = document.getElementById('dashboard-theme-link');
        if (saved && themeLink) {
            if (saved === 'midnight-blue') themeLink.setAttribute('href', 'assets/css/dashboard-theme-midnight-blue.css');
            else if (saved === 'ember-ops') themeLink.setAttribute('href', 'assets/css/dashboard-theme-ember-ops.css');
            else if (saved === 'neon-grid') themeLink.setAttribute('href', 'assets/css/dashboard-theme-neon-grid.css');
            document.documentElement.setAttribute('data-dashboard-theme', saved);
        }
    } catch {}
})();
</script>
""");

            sb.AppendLine("<div class='wrapper'>");

            return sb.ToString();
        }

        /// <summary>
        /// Sobrecarga mantida por compatibilidade com exporters antigos.
        /// </summary>
        public static string RenderDocumentStart(string title, string? themeFileName)
            => RenderDocumentStart(title);

        /// <summary>
        /// Gera a barra de navegação tática para as páginas filhas.
        /// </summary>
        /// <param name="activeModule">O nome do módulo atual (ex: "Structural", "Architectural", "Parsing", "Quality")</param>
        public static string RenderTacticalNav(string activeModule)
        {
            // Função local para definir a classe CSS baseada no módulo ativo
            string GetNavClass(string moduleName) =>
                string.Equals(activeModule, moduleName, StringComparison.OrdinalIgnoreCase)
                ? "nav-btn active"
                : "nav-btn";

            return $"""
        <div class="tactical-nav-wrapper">
            <a href="index.html" class="nav-btn btn-hub" augmented-ui="tl-clip br-clip border" title="Return to Command Nexus">
                &#9664; NEXUS_HUB
            </a>
            </div>
""";
        }

        public static string RenderDocumentEnd(string? footerText = null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(footerText))
            {
                sb.AppendLine($"<div class='footer'>{Html(footerText)}</div>");
            }

            // Lógica Global do Botão: Mapeia o click e salva no LocalStorage se a página atual possuir o botão.
            sb.AppendLine("""
<script>
(function () {
    const STORAGE_KEY = 'refactorscope-dashboard-theme';
    const themeLink = document.getElementById('dashboard-theme-link');
    const cyclerBtn = document.getElementById('themeCyclerBtn');

    const themeSequence = [
        { id: 'midnight-blue', css: 'assets/css/dashboard-theme-midnight-blue.css' },
        { id: 'ember-ops', css: 'assets/css/dashboard-theme-ember-ops.css' },
        { id: 'neon-grid', css: 'assets/css/dashboard-theme-neon-grid.css' }
    ];

    let currentIndex = 0;

    // Detecta qual o índice atual com base no que o script do <head> restaurou
    try {
        const saved = localStorage.getItem(STORAGE_KEY);
        if (saved) {
            const idx = themeSequence.findIndex(t => t.id === saved);
            if (idx !== -1) currentIndex = idx;
        }
    } catch { }

    function applyTheme(index) {
        const t = themeSequence[index];
        if (themeLink) {
            themeLink.setAttribute('href', t.css);
        }
        document.documentElement.setAttribute('data-dashboard-theme', t.id);

        try {
            localStorage.setItem(STORAGE_KEY, t.id);
        } catch { }

        // Glitch óptico de calibração do monitor
        document.body.style.display = 'none';
        void document.body.offsetHeight; 
        document.body.style.display = '';
    }

    // Se o painel atual exportou o botão, injeta o comportamento de ciclo
    if (cyclerBtn) {
        cyclerBtn.addEventListener('click', function () {
            currentIndex = (currentIndex + 1) % themeSequence.length;
            applyTheme(currentIndex);
        });
    }
})();
</script>
""");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static string RenderUnavailableNavCard(
            string moduleTag,
            string title,
            string description)
        {
            return $"""
<a class="nav-card is-disabled" aria-disabled="true" augmented-ui="tr-clip bl-clip border">
    <div class="tag">{Html(moduleTag)}</div>
    <div class="title">{Html(title)}</div>
    <div class="desc">{Html(description)}</div>
    <div class="accent"></div>
</a>
""";
        }

        public static string RenderNavCard(
            string moduleTag,
            string title,
            string description,
            string href)
        {
            return $"""
<a class="nav-card" augmented-ui="tr-clip bl-clip border" href="{Html(href)}">
    <div class="tag">{Html(moduleTag)}</div>
    <div class="title">{Html(title)}</div>
    <div class="desc">{Html(description)}</div>
    <div class="accent"></div>
</a>
""";
        }

        public static string Html(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }
}