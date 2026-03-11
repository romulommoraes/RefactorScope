namespace RefactorScope.CLI
{
    public static class SelfAnalysisSelector
    {
        public static StartupModeSelection Resolve(
            string defaultConfigPath,
            string selfConfigPath,
            bool enableInteractiveSelector)
        {
            if (!enableInteractiveSelector)
            {
                return new StartupModeSelection
                {
                    ConfigPath = defaultConfigPath,
                    IsSelfAnalysis = false,
                    IsBatchMode = false
                };
            }

            Console.WriteLine();
            Console.WriteLine("🧬 RefactorScope Startup Mode");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("1) Normal Analysis");
            Console.WriteLine("2) Self Analysis (RefactorScope analyzing itself)");
            Console.WriteLine("3) Batch Arena");
            Console.WriteLine();
            Console.Write("Select mode (1, 2 or 3) [default=1]: ");

            var input = Console.ReadLine()?.Trim();

            return input switch
            {
                "2" => ResolveSelf(selfConfigPath),
                "3" => ResolveBatch(defaultConfigPath),
                _ => ResolveNormal(defaultConfigPath)
            };
        }

        private static StartupModeSelection ResolveNormal(string configPath)
        {
            Console.WriteLine("📦 Normal Analysis Mode Enabled");

            return new StartupModeSelection
            {
                ConfigPath = configPath,
                IsSelfAnalysis = false,
                IsBatchMode = false
            };
        }

        private static StartupModeSelection ResolveSelf(string configPath)
        {
            Console.WriteLine("🔍 Self Analysis Mode Enabled");

            return new StartupModeSelection
            {
                ConfigPath = configPath,
                IsSelfAnalysis = true,
                IsBatchMode = false
            };
        }

        private static StartupModeSelection ResolveBatch(string configPath)
        {
            Console.WriteLine("🗂️ Batch Arena Mode Enabled");

            return new StartupModeSelection
            {
                ConfigPath = configPath,
                IsSelfAnalysis = false,
                IsBatchMode = true
            };
        }
    }
}