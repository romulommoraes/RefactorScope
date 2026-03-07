using RefactorScope.Core.Abstractions;
using RefactorScope.Core.Model;
using System.Text;

namespace RefactorScope.Exporters;

public sealed class ParsingDashboardExporter
{
    public void Export(IParserResult result, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var htmlPath = Path.Combine(outputDirectory, "ParsingDashboard.html");

        File.WriteAllText(
            htmlPath,
            GenerateHtml(result),
            Encoding.UTF8);
    }


    private string GenerateHtml(IParserResult result)
    {
        var files = result.Model?.Arquivos.Count ?? 0;
        var types = result.Model?.Tipos.Count ?? 0;
        var refs = result.Model?.Referencias.Count ?? 0;

        double executionMs = result.Stats?.ExecutionTime.TotalMilliseconds ?? 0;

        double classesPerFile =
            files == 0 ? 0 : types / (double)files;

        double refsPerClass =
            types == 0 ? 0 : refs / (double)types;

        double timePerClass =
            types == 0 ? 0 : executionMs / types;

        var charts = new ParsingChartsRenderer();

        var sb = new StringBuilder();

        sb.AppendLine("<html><head><meta charset='UTF-8'>");

        #region STYLE

        sb.AppendLine("<style>");

        sb.AppendLine("body { background:#0f1117; color:#e6edf3; font-family:Segoe UI; margin:30px; }");

        sb.AppendLine(".card { display:inline-block; padding:20px; margin:10px; background:#161b22; border-radius:10px; min-width:180px; }");

        sb.AppendLine(".grid { display:grid; grid-template-columns:1fr 1fr; gap:40px; margin-top:40px; }");

        sb.AppendLine("table { border-collapse: collapse; width:100%; margin-top:40px; }");
        sb.AppendLine("th, td { border:1px solid #30363d; padding:8px; }");
        sb.AppendLine("th { background:#21262d; }");

        sb.AppendLine("</style>");

        #endregion

        sb.AppendLine("</head><body>");

        sb.AppendLine("<h1>🧠 Parsing Dashboard</h1>");

        #region KPI CARDS

        sb.AppendLine("<div>");

        sb.AppendLine($"<div class='card'>Parser<br><b>{result.ParserName}</b></div>");
        sb.AppendLine($"<div class='card'>Files<br><b>{files}</b></div>");
        sb.AppendLine($"<div class='card'>Types<br><b>{types}</b></div>");
        sb.AppendLine($"<div class='card'>References<br><b>{refs}</b></div>");

        sb.AppendLine($"<div class='card'>Execution<br><b>{executionMs:0} ms</b></div>");

        sb.AppendLine($"<div class='card'>Classes/File<br><b>{classesPerFile:0.00}</b></div>");
        sb.AppendLine($"<div class='card'>Refs/Class<br><b>{refsPerClass:0.00}</b></div>");
        sb.AppendLine($"<div class='card'>Time/Class<br><b>{timePerClass:0.00} ms</b></div>");
        sb.AppendLine($"<div class='card'>Confidence<br><b>{result.Confidence:P0}</b></div>");

        sb.AppendLine("</div>");

        #endregion


        #region CHARTS

        sb.AppendLine("<div class='grid'>");

        // Structure density
        sb.AppendLine("<div>");
        sb.AppendLine(charts.RenderStructureDensity(files, types, refs));
        sb.AppendLine("</div>");

        // Radar
        sb.AppendLine("<div>");
        sb.AppendLine(
            charts.RenderParsingRadar(
                classesPerFile,
                refsPerClass,
                result.Confidence,
                timePerClass));
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");

        #endregion


        #region TABLE

        sb.AppendLine("<h2 style='margin-top:50px;'>Parsed Types</h2>");

        sb.AppendLine("<table>");
        sb.AppendLine("<tr>");
        sb.AppendLine("<th>Type</th>");
        sb.AppendLine("<th>Namespace</th>");
        sb.AppendLine("<th>File</th>");
        sb.AppendLine("<th>References</th>");
        sb.AppendLine("</tr>");

        foreach (var type in result.Model?.Tipos ?? Enumerable.Empty<TipoInfo>())
        {
            var refsCount =
                result.Model?.Referencias?
                    .Count(r => r.FromType == type.Name) ?? 0;

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{type.Name}</td>");
            sb.AppendLine($"<td>{type.Namespace}</td>");
            sb.AppendLine($"<td>{type.DeclaredInFile}</td>");
            sb.AppendLine($"<td>{refsCount}</td>");
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table>");

        #endregion


        sb.AppendLine("</body></html>");

        return sb.ToString();
    }
}