namespace RefactorScope.Exporters.Infrastructure;

public sealed class ExportFolderBootstrapper
{
    private readonly ExportPathResolver _paths;

    public ExportFolderBootstrapper(ExportPathResolver paths)
    {
        _paths = paths;
    }

    public void EnsureStructure()
    {
        Directory.CreateDirectory(_paths.RootOutputPath);
    }
}