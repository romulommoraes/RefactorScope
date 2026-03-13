namespace RefactorScope.CLI;

/// <summary>
/// Resolves the effective C# project root.
/// 
/// Rule:
/// - if the configured path already contains a .csproj, use it
/// - otherwise, search downward and select the nearest directory
///   that contains a .csproj
/// - fallback to the original path if nothing is found
/// </summary>
public static class ProjectRootResolver
{
    public static string ResolveEffectiveProjectRoot(string configuredRootPath)
    {
        var fullRootPath = Path.GetFullPath(configuredRootPath);

        if (!Directory.Exists(fullRootPath))
            return fullRootPath;

        var csprojFilesInRoot = Directory.GetFiles(
            fullRootPath,
            "*.csproj",
            SearchOption.TopDirectoryOnly);

        if (csprojFilesInRoot.Length > 0)
            return fullRootPath;

        var descendantProjectDirectories = Directory
            .GetFiles(fullRootPath, "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x!.Length)
            .ToList();

        if (descendantProjectDirectories.Count > 0)
            return descendantProjectDirectories[0]!;

        return fullRootPath;
    }
}