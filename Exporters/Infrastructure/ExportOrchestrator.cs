namespace RefactorScope.Exporters.Infrastructure;

public sealed class ExportOrchestrator
{
    private readonly ExportOptions _options;
    private readonly ExportPathResolver _paths;
    private readonly ExportFolderBootstrapper _folders;

    public ExportOrchestrator(ExportOptions options, ExportPathResolver paths)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _folders = new ExportFolderBootstrapper(_paths);
    }

    public void Prepare()
    {
        if (!_options.Enabled)
            return;

        _folders.EnsureStructure();
    }
}