using Spectre.Console;

namespace RefactorScope.Infrastructure
{
    public static class TerminalRenderer
    {
        public static void ShowHeader(string root)
        {
            var panel = new Panel($"[bold yellow]{root}[/]")
                .Header("[green]RefactorScope v1.0[/]")
                .Border(BoxBorder.Rounded);

            AnsiConsole.Write(panel);
        }

        public static void Step(string message)
        {
            AnsiConsole.MarkupLine($"[blue]→[/] {message}");
        }

        public static void Success(string message)
        {
            AnsiConsole.MarkupLine($"[green]✔[/] {message}");
        }

        public static void Section(string title)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                new Rule($"[bold]{title}[/]").RuleStyle("grey").Centered()
            );
        }

        public static void TableSummary(int zombies, int isolated, int entryPoints)
        {
            var table = new Table();

            table.AddColumn("Métrica");
            table.AddColumn("Valor");

            table.AddRow("Zombies", zombies.ToString());
            table.AddRow("Core Isolado", isolated.ToString());
            table.AddRow("Entry Points", entryPoints.ToString());

            AnsiConsole.Write(table);
        }

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
    }
}