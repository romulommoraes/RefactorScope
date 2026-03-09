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
    ///
    /// Benefícios
    /// ----------
    /// - padronização visual
    /// - troca de tema sem alterar exporters
    /// - menor duplicação de markup
    /// - evolução mais segura da suíte HTML
    ///
    /// Estratégia de tema
    /// ------------------
    /// O shell sempre referencia:
    /// - dashboard-base.css
    /// - dashboard-components.css
    /// - dashboard-theme.css
    ///
    /// O arquivo dashboard-theme.css é gerado pelo pipeline de assets
    /// a partir do tema lógico selecionado em configuração.
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

            // Vendor CSS local
            sb.AppendLine("<link rel='stylesheet' href='assets/vendor/Charts.css' />");
            sb.AppendLine("<link rel='stylesheet' href='assets/vendor/augmented-ui.css' />");

            // Shared dashboard CSS
            sb.AppendLine("<link rel='stylesheet' href='assets/css/dashboard-base.css' />");
            sb.AppendLine("<link rel='stylesheet' href='assets/css/dashboard-components.css' />");
            sb.AppendLine("<link rel='stylesheet' href='assets/css/dashboard-theme.css' />");

            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='wrapper'>");

            return sb.ToString();
        }

        /// <summary>
        /// Sobrecarga mantida por compatibilidade com exporters antigos.
        ///
        /// O parâmetro themeFileName é ignorado, pois o shell agora
        /// referencia sempre o arquivo fixo dashboard-theme.css.
        /// </summary>
        public static string RenderDocumentStart(string title, string? themeFileName)
            => RenderDocumentStart(title);

        public static string RenderDocumentEnd(string? footerText = null)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(footerText))
            {
                sb.AppendLine($"<div class='footer'>{Html(footerText)}</div>");
            }

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