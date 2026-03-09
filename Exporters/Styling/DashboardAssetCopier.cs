using System;

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
    ///
    /// Estratégia de tema
    /// ------------------
    /// O sistema copia:
    /// - dashboard-base.css
    /// - dashboard-components.css
    /// - o tema resolvido pelo DashboardThemeSelector
    ///
    /// O tema selecionado é copiado para a saída com o nome fixo:
    /// - dashboard-theme.css
    ///
    /// Isso permite que o HTML referencie sempre o mesmo arquivo,
    /// independentemente do tema escolhido em configuração.
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

            Directory.CreateDirectory(targetCssDir);
            Directory.CreateDirectory(targetVendorDir);

            CopyVendorAssets(sourceVendorDir, targetVendorDir);
            CopyBaseCssAssets(sourceCssDir, targetCssDir);
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

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                "Não foi possível localizar a pasta Exporters/Assets no projeto.");
        }

        private static void CopyVendorAssets(string sourceVendorDir, string targetVendorDir)
        {
            CopyDirectory(sourceVendorDir, targetVendorDir);
        }

        private static void CopyBaseCssAssets(string sourceCssDir, string targetCssDir)
        {
            CopySingleFile(sourceCssDir, targetCssDir, "dashboard-base.css");
            CopySingleFile(sourceCssDir, targetCssDir, "dashboard-components.css");
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

        private static void CopySingleFile(string sourceDir, string targetDir, string fileName)
        {
            var sourcePath = Path.Combine(sourceDir, fileName);
            var targetPath = Path.Combine(targetDir, fileName);

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException(
                    $"Arquivo de asset não encontrado: {fileName}",
                    sourcePath);
            }

            File.Copy(sourcePath, targetPath, overwrite: true);
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