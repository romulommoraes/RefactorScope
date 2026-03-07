using System.Globalization;
using System.Text;

namespace RefactorScope.Exporters
{
    /// <summary>
    /// Renderer responsável pelos gráficos do Parsing Dashboard.
    ///
    /// Objetivo:
    /// Visualizar qualidade e eficiência do parser estrutural.
    ///
    /// Gráficos disponíveis:
    ///
    /// 1. Structure Density
    ///    - Files
    ///    - Types
    ///    - References
    ///
    /// 2. Parsing Quality Radar
    ///    - Classes per File
    ///    - References per Class
    ///    - Confidence
    ///    - Time per Class (eficiência do parser)
    ///
    /// Os gráficos são renderizados como SVG inline
    /// para máxima portabilidade e zero dependência JS.
    /// </summary>
    public class ParsingChartsRenderer
    {
        /// <summary>
        /// Tamanho base dos gráficos.
        /// Mantido consistente com o Structural Dashboard.
        /// </summary>
        const int ChartSize = 320;


        /// <summary>
        /// Renderiza gráfico de densidade estrutural.
        ///
        /// Mostra:
        /// - quantidade de arquivos analisados
        /// - número de tipos detectados
        /// - total de referências estruturais
        ///
        /// Valores absolutos são exibidos acima das barras.
        /// Altura das barras é normalizada pelo maior valor.
        /// </summary>
        public string RenderStructureDensity(
            int files,
            int types,
            int refs)
        {
            string Fmt(double v) => v.ToString(CultureInfo.InvariantCulture);

            int width = ChartSize;
            int height = ChartSize;
            int margin = 40;

            int max = Math.Max(files, Math.Max(types, refs));

            double Normalize(int v) =>
                max == 0 ? 0 : v / (double)max;

            double barMaxHeight = height - margin * 2;

            double barFiles = barMaxHeight * Normalize(files);
            double barTypes = barMaxHeight * Normalize(types);
            double barRefs = barMaxHeight * Normalize(refs);

            double spacing = 70;

            double x1 = margin + spacing;
            double x2 = x1 + spacing;
            double x3 = x2 + spacing;

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Structure Density</h3>");

            sb.AppendLine(
                $"<svg width='{width}' height='{height}' style='background:#161b22;border-radius:12px'>");

            // eixo
            sb.AppendLine(
                $"<line x1='{margin}' y1='{height - margin}' x2='{width - margin}' y2='{height - margin}' stroke='#30363d'/>");

            // barras
            sb.AppendLine(
                $"<rect x='{Fmt(x1)}' y='{Fmt(height - margin - barFiles)}' width='30' height='{Fmt(barFiles)}' fill='#4f8cff'/>");

            sb.AppendLine(
                $"<rect x='{Fmt(x2)}' y='{Fmt(height - margin - barTypes)}' width='30' height='{Fmt(barTypes)}' fill='#1dd1a1'/>");

            sb.AppendLine(
                $"<rect x='{Fmt(x3)}' y='{Fmt(height - margin - barRefs)}' width='30' height='{Fmt(barRefs)}' fill='#ffd166'/>");

            // valores numéricos
            sb.AppendLine(
                $"<text x='{Fmt(x1 + 15)}' y='{Fmt(height - margin - barFiles - 6)}' text-anchor='middle' fill='#e6edf3' font-size='11'>{files}</text>");

            sb.AppendLine(
                $"<text x='{Fmt(x2 + 15)}' y='{Fmt(height - margin - barTypes - 6)}' text-anchor='middle' fill='#e6edf3' font-size='11'>{types}</text>");

            sb.AppendLine(
                $"<text x='{Fmt(x3 + 15)}' y='{Fmt(height - margin - barRefs - 6)}' text-anchor='middle' fill='#e6edf3' font-size='11'>{refs}</text>");

            // labels
            sb.AppendLine(
                $"<text x='{Fmt(x1 + 15)}' y='{height - 10}' text-anchor='middle' fill='#e6edf3' font-size='11'>Files</text>");

            sb.AppendLine(
                $"<text x='{Fmt(x2 + 15)}' y='{height - 10}' text-anchor='middle' fill='#e6edf3' font-size='11'>Types</text>");

            sb.AppendLine(
                $"<text x='{Fmt(x3 + 15)}' y='{height - 10}' text-anchor='middle' fill='#e6edf3' font-size='11'>Refs</text>");

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }



        /// <summary>
        /// Renderiza radar de qualidade do parsing.
        ///
        /// Métricas utilizadas:
        ///
        /// Classes/File
        /// densidade estrutural média.
        ///
        /// Refs/Class
        /// riqueza do grafo estrutural.
        ///
        /// Confidence
        /// plausibilidade heurística do parser.
        ///
        /// Time/Class
        /// eficiência do parser independente do tamanho do projeto.
        ///
        /// Todas as métricas são normalizadas para intervalo [0,1].
        /// </summary>
        public string RenderParsingRadar(
            double classesPerFile,
            double refsPerClass,
            double confidence,
            double timePerClassMs)
        {
            string Fmt(double v) => v.ToString(CultureInfo.InvariantCulture);

            int size = ChartSize;
            int center = size / 2;
            int radius = 110;
            int levels = 4;

            // normalizações empíricas
            double normClasses = Math.Min(1, classesPerFile / 2.0);
            double normRefs = Math.Min(1, refsPerClass / 10.0);
            double normConfidence = Math.Min(1, confidence);

            // 50ms/class considerado limite alto
            double normTime = Math.Min(1, timePerClassMs / 50.0);

            var values = new[]
            {
                normClasses,
                normRefs,
                normConfidence,
                normTime
            };

            var labels = new[]
            {
                "Classes/File",
                "Refs/Class",
                "Confidence",
                "Time/Class"
            };

            var sb = new StringBuilder();

            sb.AppendLine("<div class='chart-container'>");
            sb.AppendLine("<h3>Parsing Quality Radar</h3>");

            sb.AppendLine(
                $"<svg width='{size}' height='{size}' style='background:#161b22;border-radius:12px'>");

            // círculos
            for (int l = 1; l <= levels; l++)
            {
                double rr = radius * (l / (double)levels);

                sb.AppendLine(
                    $"<circle cx='{center}' cy='{center}' r='{Fmt(rr)}' fill='none' stroke='#30363d' stroke-width='1' />");
            }

            // eixos
            for (int i = 0; i < values.Length; i++)
            {
                double angle =
                    (Math.PI * 2 / values.Length) * i - Math.PI / 2;

                double x = center + radius * Math.Cos(angle);
                double y = center + radius * Math.Sin(angle);

                sb.AppendLine(
                    $"<line x1='{center}' y1='{center}' x2='{Fmt(x)}' y2='{Fmt(y)}' stroke='#30363d' />");

                double lx = center + (radius + 22) * Math.Cos(angle);
                double ly = center + (radius + 22) * Math.Sin(angle);

                sb.AppendLine(
                    $"<text x='{Fmt(lx)}' y='{Fmt(ly)}' fill='#e6edf3' font-size='11' text-anchor='middle'>{labels[i]}</text>");
            }

            // pontos do polígono
            var points = new List<string>();

            for (int i = 0; i < values.Length; i++)
            {
                double angle =
                    (Math.PI * 2 / values.Length) * i - Math.PI / 2;

                double rVal = radius * values[i];

                double x = center + rVal * Math.Cos(angle);
                double y = center + rVal * Math.Sin(angle);

                points.Add($"{Fmt(x)},{Fmt(y)}");
            }

            sb.AppendLine(
                $"<polygon points='{string.Join(" ", points)}' fill='rgba(88,166,255,0.35)' stroke='#58a6ff' stroke-width='2'/>");

            sb.AppendLine("</svg>");
            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}