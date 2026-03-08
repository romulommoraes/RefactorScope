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
    /// </summary>
    public static class DashboardHtmlShell
    {
        public static string RenderDocumentStart(
            string title,
            string themeFileName)
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
            sb.AppendLine($"<link rel='stylesheet' href='assets/css/{Html(themeFileName)}' />");

            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='wrapper'>");

            return sb.ToString();
        }

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