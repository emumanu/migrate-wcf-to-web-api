using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MigrateWcfToWebApi.Core.Models;
using static System.Console;

namespace MigrateWcfToWebApi.Utils
{
    internal static class ConsoleMessaging
    {
        public static void PrintGeneratedFilepaths(List<string> filepaths)
        {
            WriteLine();
            foreach (var file in filepaths)
            {
                WriteLine(Path.GetFullPath(file));
            }

            WriteLine();
            WriteLine($"{filepaths.Count} file(s) created.");
            WriteLine();
        }

        public static async Task PrintMethodCounts(Dictionary<string, Task<WcfServiceClassInfo>> wcfClasses)
        {
            int totalCount = 0;

            WriteLine();
            foreach (var info in wcfClasses)
            {
                var wcfClass = await info.Value;
                string wcfClassName = wcfClass.ControllerName;
                int methodsCount = wcfClass.WcfMethods.Count;

                WriteLine($"{wcfClassName} methods count: {methodsCount}");

                totalCount += methodsCount;
            }

            WriteLine();
            WriteLine($"{totalCount} method(s) to convert from wcf to web api.");
            WriteLine();
        }

        public static void PrintFileNotFoundError(string filepath, string errorMessage, bool isWarning = false)
        {
            var fullpath = Path.GetFullPath(filepath);
            string severityLevel = isWarning
                ? "WARNING"
                : "ERROR";

            var message = $"\n[{severityLevel}]: {errorMessage}: {fullpath}\n";

            WriteLine(message);
        }
    }
}