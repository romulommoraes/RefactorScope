using RefactorScope.Core.Model;
using Spectre.Console;
using RefactorScope.Estimation.Models;

namespace RefactorScope.Infrastructure
{
    public static class TerminalRenderer
    {
        // -------------------------------------------------
        // Backwards compatibility
        // -------------------------------------------------

        public static void Step(string message) => Stage(message);
        public static void Success(string message) => StageSuccess(message);
        public static void Warn(string message) => StageWarning(message);

        public static void Section(string title)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Rule($"[bold]{title}[/]")
                    .RuleStyle("grey")
                    .Centered());
        }

        // -------------------------------------------------
        // Header
        // -------------------------------------------------

        public static void ShowHeader(string root)
        {
            var panel = new Panel($"[bold yellow]{root}[/]")
                .Header("[bold green]RefactorScope v1.0[/]")
                .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        // -------------------------------------------------
        // Pipeline Stages
        // -------------------------------------------------

        public static void Stage(string name)
        {
            AnsiConsole.MarkupLine($"[bold cyan]> {name}[/]");
        }

        public static void StageSuccess(string message)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] {message}");
        }

        public static void StageWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]![/] {message}");
        }

        public static void Info(string message)
        {
            AnsiConsole.MarkupLine($"[grey]i[/] {message}");
        }

        // -------------------------------------------------
        // Parsing
        // -------------------------------------------------

        public static void ParsingStrategy(string parserName)
        {
            var friendly = ResolveParserDisplayName(parserName);

            AnsiConsole.MarkupLine(
                $"[grey]Parser[/]: [bold cyan]{friendly}[/]");
        }

        public static void ParsingFallback(string fromParser, string toParser)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Fallback[/]: {ResolveParserDisplayName(fromParser)} → {ResolveParserDisplayName(toParser)}");
        }

        public static void ParsingMerge()
        {
            AnsiConsole.MarkupLine(
                $"[magenta]Merge[/]: consolidating structural model + dependency signals");
        }

        public static void ParsingSummary(
            int files,
            int types,
            int references,
            TimeSpan execution)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("[bold]Metric[/]")
                .AddColumn("[bold]Value[/]");

            table.AddRow("Files", files.ToString());
            table.AddRow("Types", types.ToString());
            table.AddRow("References", references.ToString());
            table.AddRow("Execution", $"{execution.TotalMilliseconds:0} ms");

            AnsiConsole.Write(table);
        }

        // -------------------------------------------------
        // Structural Validation
        // -------------------------------------------------

        public static void StructuralValidationSummary(
            int structuralCandidates,
            int confirmedUnresolved,
            int filteredByValidation,
            double reductionRate,
            double threshold)
        {
            var unresolvedColor = confirmedUnresolved == 0 ? "green" : "red";
            var reductionColor = reductionRate >= 0.75
                ? "green"
                : reductionRate >= 0.40
                    ? "yellow"
                    : "red";

            var panel = new Panel(
                $"[bold]Structural Candidates[/]: {structuralCandidates}\n" +
                $"[bold]Confirmed Unresolved (≥ {threshold:0.00})[/]: [{unresolvedColor}]{confirmedUnresolved}[/]\n" +
                $"[bold]Filtered by Validation[/]: {filteredByValidation}\n" +
                $"[bold]Reduction[/]: [{reductionColor}]{reductionRate * 100:0.0}%[/]"
            )
            .Header("[bold yellow]Structural Validation[/]")
            .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
        }

        // -------------------------------------------------
        // Architecture Health (TABLE)
        // -------------------------------------------------

        public static void RenderArchitecturalHealthTable(
            IEnumerable<(string Module,
                         double Score,
                         string Unresolved,
                         double Coupling,
                         double Isolation)> rows)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Expand();

            table.AddColumn("[bold]Module[/]");
            table.AddColumn("[bold]Score[/]");
            table.AddColumn("[bold]Unresolved[/]");
            table.AddColumn("[bold]Coupling[/]");
            table.AddColumn("[bold]Isolation[/]");

            foreach (var r in rows)
            {
                var scoreColor = ResolveScoreColor(r.Score);
                var unresolvedColor = r.Unresolved.StartsWith("0") ? "green" : "red";
                var moduleColor = ResolveModuleColor(r.Module);

                table.AddRow(
                    $"[{moduleColor}]{r.Module}[/]",
                    $"[{scoreColor}]{r.Score:0.0}[/]",
                    $"[{unresolvedColor}]{r.Unresolved}[/]",
                    $"{r.Coupling:0.00}",
                    $"{r.Isolation:0.00}"
                );
            }

            AnsiConsole.Write(table);
        }

        public static void CouplingHeuristicNotice()
        {
            Info("Unresolved items remained after probabilistic validation and require manual review.");
            Info("Coupling for tooling modules is heuristically softened and should be interpreted with caution.");
        }

        // -------------------------------------------------
        // Hygiene
        // -------------------------------------------------

        public static void HygieneSummary(HygieneReport hygiene)
        {
            var smellColor = ResolveSmellColor(hygiene.SmellIndex);

            var panel = new Panel(
                $"[bold]Code Smell Index[/]: [{smellColor}]{hygiene.SmellIndex:0.0}[/]\n" +
                $"[bold]Status[/]: {ResolveHygieneStatus(hygiene.HygieneLevel)}\n\n" +
                $"[bold]Dead Code Candidates[/]: {hygiene.UnreferencedCount}\n" +
                $"[bold]Namespace Drift[/]: {hygiene.NamespaceDriftCount}\n" +
                $"[bold]Global Namespace Types[/]: {hygiene.GlobalNamespaceCount}\n" +
                $"[bold]Core Isolation Flags[/]: {hygiene.IsolatedCoreCount}"
            )
            .Header("[bold yellow]Code Hygiene[/]")
            .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
        }

        // -------------------------------------------------
        // Effort Estimation
        // -------------------------------------------------

        public static void EstimationSummary(EffortEstimate estimate)
        {
            AnsiConsole.WriteLine();

            var panel = new Panel(
                $"[bold]RDI[/]: {estimate.RDI}\n" +
                $"[bold]Difficulty[/]: {estimate.Difficulty}\n" +
                $"[bold]Estimated Hours[/]: {estimate.EstimatedHours:0.0}\n" +
                $"[bold]Confidence[/]: {estimate.Confidence:0.00}")
            .Header("[bold yellow]Refactor Effort Estimation[/]")
            .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
        }

        public static void RenderEffortEstimate(EffortEstimate estimate)
        {
            var difficultyColor = estimate.Difficulty switch
            {
                "Low" => "green",
                "Medium" => "yellow",
                "High" => "orange1",
                _ => "red"
            };

            var effortBar = BuildEffortBar(estimate.EstimatedHours);

            var panel = new Panel(
                $"[bold]RDI[/]: {estimate.RDI}\n" +
                $"[bold]Difficulty[/]: [{difficultyColor}]{estimate.Difficulty}[/]\n" +
                $"[bold]Estimated Hours[/]: {estimate.EstimatedHours:0.0}\n" +
                $"[bold]Confidence[/]: {estimate.Confidence:0.00}\n\n" +
                $"[bold]Effort[/]: {effortBar}"
            )
            .Header("[bold yellow]Refactor Effort Estimation[/]")
            .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
        }

        // -------------------------------------------------
        // Spinner
        // -------------------------------------------------

        public static T WithSpinner<T>(string message, Func<T> action)
        {
            return AnsiConsole.Status()
                .Start(message, ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    return action();
                });
        }

        // -------------------------------------------------
        // Module Ordering
        // -------------------------------------------------

        public static IEnumerable<IGrouping<string, T>> OrderModules<T>(
            IEnumerable<IGrouping<string, T>> modules)
        {
            return modules
                .OrderBy(m => m.Key, StringComparer.OrdinalIgnoreCase);
        }

        // -------------------------------------------------
        // Helpers
        // -------------------------------------------------

        private static string ResolveModuleColor(string module)
        {
            return module.ToLower() switch
            {
                "core" => "cyan",
                "analyzers" => "green",
                "parsers" => "yellow",
                "statistics" => "magenta",
                "cli" => "orange1",
                "debug" => "grey",
                "exporters" => "deepskyblue1",
                "execution" => "steelblue1",
                "infrastructure" => "gold1",
                "datasets" => "darkseagreen1",
                "metrics" => "mediumpurple",
                "model" => "cadetblue1",
                "reporting" => "lightpink1",
                "scope" => "turquoise2",
                _ => "white"
            };
        }

        private static string ResolveScoreColor(double score)
        {
            return score switch
            {
                >= 80 => "green",
                >= 60 => "yellow",
                _ => "red"
            };
        }

        private static string ResolveSmellColor(double smell)
        {
            return smell switch
            {
                <= 20 => "green",
                <= 40 => "yellow",
                <= 60 => "orange1",
                <= 80 => "red",
                _ => "maroon"
            };
        }

        private static string ResolveParserDisplayName(string parserName)
        {
            return parserName switch
            {
                "CSharpRegex" => "Regex Fast Scan",
                "HybridSelectiveParser" => "Hybrid Selective (Accurate Scan)",
                "HybridParser (Adaptive)" => "Hybrid Adaptive (Experimental)",
                "HybridParser (Incremental)" => "Hybrid Incremental (Experimental)",
                "HybridParser (Incremental → Regex)" => "Hybrid Incremental → Regex",
                _ => parserName
            };
        }

        private static string ResolveHygieneStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "[grey]Unknown[/]";

            return value.ToLower() switch
            {
                "healthy" => "[green]Healthy[/]",
                "stable" => "[yellow]Stable[/]",
                "warning" => "[orange1]Warning[/]",
                "critical" => "[red]Critical[/]",
                _ => value
            };
        }

        private static string BuildEffortBar(double hours)
        {
            const int maxHours = 80;
            const int width = 20;

            var normalized = Math.Min(hours / maxHours, 1.0);

            int filled = (int)Math.Round(normalized * width);
            int empty = width - filled;

            return $"[orange1]{new string('█', filled)}[/][grey]{new string('░', empty)}[/]";
        }
    }
}