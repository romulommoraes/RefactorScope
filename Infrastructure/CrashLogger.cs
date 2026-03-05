using System;
using System.Collections.Generic;
using System.Text;

namespace RefactorScope.Infrastructure
{
    static class CrashLogger
    {
        private const string LogFile = "refactorscope-crash.log";

        public static void Log(Exception ex, string phase)
        {
            try
            {
                var log = $"""
================================================
RefactorScope Crash
Timestamp: {DateTime.Now}
Phase: {phase}

{ex}

================================================

""";

                File.AppendAllText(LogFile, log);
            }
            catch
            {
                // Nunca deixar o logger causar outro crash
            }
        }
    }
}
