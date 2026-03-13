namespace RefactorScope.Exporters.Infrastructure;

public sealed class ExportPathResolver
{
    public string RootOutputPath { get; }

    public ExportPathResolver(string rootOutputPath)
    {
        if (string.IsNullOrWhiteSpace(rootOutputPath))
            throw new ArgumentException("Root output path cannot be null or empty.", nameof(rootOutputPath));

        RootOutputPath = rootOutputPath;
    }

    public string GetAssetsDirectory()
        => Path.Combine(RootOutputPath, "assets");

    public string GetDatasetsDirectory()
        => Path.Combine(RootOutputPath, "datasets");

    public string GetCsvDatasetsDirectory()
        => Path.Combine(GetDatasetsDirectory(), "csvs");

    public string GetDumpsDirectory()
        => Path.Combine(RootOutputPath, "dumps");

    public string GetTrendsDirectory()
        => Path.Combine(RootOutputPath, "trends");

    public string GetRootFilePath(string fileName)
        => Path.Combine(RootOutputPath, fileName);

    public string GetDatasetJsonPath(string fileName)
        => Path.Combine(GetDatasetsDirectory(), fileName);

    public string GetDatasetCsvPath(string fileName)
        => Path.Combine(GetCsvDatasetsDirectory(), fileName);

    public string GetTrendFilePath(string fileName)
        => Path.Combine(GetTrendsDirectory(), fileName);

    public string GetDumpFilePath(string fileName)
        => Path.Combine(GetDumpsDirectory(), fileName);

    public string BuildTimestampedDumpFileName(string baseName, DateTime timestampUtc)
    {
        var stamp = timestampUtc.ToString("yyyyMMdd_HHmmss");
        return $"{baseName}_{stamp}.json";
    }
}