namespace RefactorScope.Exporters.Styling
{
    /// <summary>
    /// Copia os assets visuais dos dashboards para a pasta de saída.
    ///
    /// Origem esperada no repositório:
    ///
    /// RefactorScope/
    ///   Exporters/
    ///     Assets/
    ///       Css/
    ///       Vendor/
    ///
    /// Destino na publicação:
    ///
    /// <output>/assets/css/
    /// <output>/assets/vendor/
    /// </summary>
    public static class DashboardAssetCopier
    {
        public static void CopyAll(string outputPath, string themeFileName)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path inválido.", nameof(outputPath));

            var sourceRoot = ResolveAssetsRoot();
            var sourceCssDir = Path.Combine(sourceRoot, "Css");
            var sourceVendorDir = Path.Combine(sourceRoot, "Vendor");

            var targetAssetsDir = Path.Combine(outputPath, "assets");
            var targetCssDir = Path.Combine(targetAssetsDir, "css");
            var targetVendorDir = Path.Combine(targetAssetsDir, "vendor");

            Directory.CreateDirectory(targetCssDir);
            Directory.CreateDirectory(targetVendorDir);

            CopyDirectory(sourceCssDir, targetCssDir);
            CopyDirectory(sourceVendorDir, targetVendorDir);

            EnsureRequiredCssExists(targetCssDir, themeFileName);
        }

        private static string ResolveAssetsRoot()
        {
            var baseDir = AppContext.BaseDirectory;
            var dir = new DirectoryInfo(baseDir);

            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName,
                    "RefactorScope",
                    "Exporters",
                    "Assets");

                if (Directory.Exists(candidate))
                    return candidate;

                candidate = Path.Combine(
                    dir.FullName,
                    "Exporters",
                    "Assets");

                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Não foi possível localizar a pasta Exporters/Assets no projeto.");
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException(
                    $"Pasta de assets não encontrada: {sourceDir}");

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destination = Path.Combine(targetDir, fileName);

                File.Copy(file, destination, overwrite: true);
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(directory);
                var destination = Path.Combine(targetDir, name);

                CopyDirectory(directory, destination);
            }
        }

        private static void EnsureRequiredCssExists(string targetCssDir, string themeFileName)
        {
            EnsureFileExists(targetCssDir, "dashboard-base.css");
            EnsureFileExists(targetCssDir, "dashboard-components.css");

            if (!string.IsNullOrWhiteSpace(themeFileName))
            {
                EnsureFileExists(targetCssDir, themeFileName);
            }
        }

        private static void EnsureFileExists(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Arquivo de asset não encontrado na pasta de saída: {fileName}",
                    path);
            }
        }
    }
}