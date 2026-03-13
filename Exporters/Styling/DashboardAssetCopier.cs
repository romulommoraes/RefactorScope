using System;

namespace RefactorScope.Exporters.Styling
{
    /// <summary>
    /// Copia os assets visuais dos dashboards para a pasta de saída.
    ///
    /// Origem preferencial:
    ///
    /// RefactorScope/
    ///   Exporters/
    ///     Assets/
    ///       Css/
    ///       Vendor/
    ///
    /// Fallback aceito:
    ///
    /// RefactorScope/
    ///   Batch/
    ///     Assets/
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

            if (string.IsNullOrWhiteSpace(themeFileName))
                themeFileName = DashboardThemeSelector.DefaultThemeFile;

            var sourceRoot = ResolveAssetsRoot();
            var sourceCssDir = Path.Combine(sourceRoot, "Css");
            var sourceVendorDir = Path.Combine(sourceRoot, "Vendor");

            var targetAssetsDir = Path.Combine(outputPath, "assets");
            var targetCssDir = Path.Combine(targetAssetsDir, "css");
            var targetVendorDir = Path.Combine(targetAssetsDir, "vendor");

            Directory.CreateDirectory(outputPath);
            Directory.CreateDirectory(targetCssDir);
            Directory.CreateDirectory(targetVendorDir);

            // Copia tudo de vendor
            CopyDirectory(sourceVendorDir, targetVendorDir);

            // Copia tudo de css, inclusive todos os temas e quaisquer arquivos extras
            CopyDirectory(sourceCssDir, targetCssDir);

            // Gera também o alias fixo usado pelo shell HTML
            CopyResolvedTheme(sourceCssDir, targetCssDir, themeFileName);

            EnsureRequiredCssExists(targetCssDir);
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

                candidate = Path.Combine(
                    dir.FullName,
                    "RefactorScope",
                    "Batch",
                    "Assets");

                if (Directory.Exists(candidate))
                    return candidate;

                candidate = Path.Combine(
                    dir.FullName,
                    "Batch",
                    "Assets");

                if (Directory.Exists(candidate))
                    return candidate;

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Não foi possível localizar a pasta de assets. Caminhos testados: Exporters/Assets e Batch/Assets.");
        }

        private static void CopyResolvedTheme(string sourceCssDir, string targetCssDir, string themeFileName)
        {
            var sourceThemePath = Path.Combine(sourceCssDir, themeFileName);
            var targetThemePath = Path.Combine(targetCssDir, "dashboard-theme.css");

   

            if (!File.Exists(sourceThemePath))
            {
                throw new FileNotFoundException(
                    $"Arquivo de tema não encontrado nos assets: {themeFileName}",
                    sourceThemePath);
            }

            File.Copy(sourceThemePath, targetThemePath, overwrite: true);
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

        private static void EnsureRequiredCssExists(string targetCssDir)
        {
            EnsureFileExists(targetCssDir, "dashboard-base.css");
            EnsureFileExists(targetCssDir, "dashboard-components.css");
            EnsureFileExists(targetCssDir, "dashboard-theme.css");
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