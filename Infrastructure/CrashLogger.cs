namespace RefactorScope.Infrastructure
{
    static class CrashLogger
    {
        private const string LogDir = "Output/log";
        private const int MaxLogs = 5;

        public static void Log(Exception ex, string phase)
        {
            try
            {
                Directory.CreateDirectory(LogDir);

                var file = Path.Combine(
                    LogDir,
                    $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log"
                );

                var content = $"""
================================================
RefactorScope Crash Report
Timestamp: {DateTime.Now}
Phase: {phase}

{ex}

================================================
""";

                File.WriteAllText(file, content);

                RotateLogs();
            }
            catch
            {
                // nunca crashar
            }
        }

        private static void RotateLogs()
        {
            var files = new DirectoryInfo(LogDir)
                .GetFiles("*.log")
                .OrderByDescending(f => f.CreationTime)
                .ToList();

            if (files.Count <= MaxLogs)
                return;

            foreach (var file in files.Skip(MaxLogs))
                file.Delete();
        }
    }
}