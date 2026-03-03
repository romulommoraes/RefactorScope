namespace RefactorScope.CLI
{
    public static class SelfAnalysisSelector
    {
        public static string ResolveConfigPath(
            string defaultConfigPath,
            string selfConfigPath,
            bool enableInteractiveSelector)
        {
            if (!enableInteractiveSelector)
                return defaultConfigPath;

            Console.WriteLine();
            Console.WriteLine("🧬 RefactorScope Startup Mode");
            Console.WriteLine("---------------------------------");
            Console.WriteLine("1) Normal Analysis");
            Console.WriteLine("2) Self Analysis (RefactorScope analyzing itself)");
            Console.WriteLine();
            Console.Write("Select mode (1 or 2) [default=1]: ");

            var input = Console.ReadLine();

            if (input?.Trim() == "2")
            {
                Console.WriteLine("🔍 Self Analysis Mode Enabled");
                return selfConfigPath;
            }

            Console.WriteLine("📦 Normal Analysis Mode Enabled");
            return defaultConfigPath;
        }
    }
}