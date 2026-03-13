namespace RefactorScope.Exporters.Infrastructure;

public sealed class ExportOptions
{
    public bool Enabled { get; init; } = true;
    public string OutputDirectory { get; init; } = "refactorscope-output";

    public ReportExportOptions Reports { get; init; } = new();
    public DatasetExportOptions Datasets { get; init; } = new();
    public DumpExportOptions Dumps { get; init; } = new();
    public TrendExportOptions Trends { get; init; } = new();
}

public sealed class ReportExportOptions
{
    public bool GenerateHubHtml { get; init; } = true;
    public bool GenerateDashboardsHtml { get; init; } = true;
    public bool GenerateExecutiveReport { get; init; } = true;
    public bool GenerateArchitecturalReport { get; init; } = true;
}

public sealed class DatasetExportOptions
{
    public bool GenerateAnalysisJson { get; init; } = true;
    public bool GenerateSnapshotJson { get; init; } = true;
    public bool GenerateCsvs { get; init; } = true;
}

public sealed class DumpExportOptions
{
    public bool Enabled { get; init; } = true;
    public bool GenerateFullDump { get; init; } = true;
    public bool IncludeTimestampInFileName { get; init; } = true;
    public bool NormalizeWhitespace { get; init; } = true;
}

public sealed class TrendExportOptions
{
    public bool Enabled { get; init; } = true;
    public bool GenerateStructuralHistory { get; init; } = true;
}