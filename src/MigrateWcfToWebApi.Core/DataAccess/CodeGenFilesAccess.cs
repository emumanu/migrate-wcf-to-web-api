using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MigrateWcfToWebApi.Core.DataAccess
{
    public static class CodeGenFilesAccess
    {
        public static async Task<List<string>> WriteServiceFiles(string serviceOutputDir, IDictionary<string, string> fileCodes)
        {
            var paths = await WriteFiles(serviceOutputDir, fileCodes, "");

            return paths;
        }

        public static async Task<List<string>> WriteClientFiles(string clientOutputDir, IDictionary<string, string> fileCodes)
        {
            var paths = await WriteFiles(clientOutputDir, fileCodes, "");

            return paths;
        }

        private static async Task<List<string>> WriteFiles(string targetDirectory, IDictionary<string, string> fileCodes, string codeGenDirectory)
        {
            string targetFilepath = Path.Combine(targetDirectory, codeGenDirectory);
            Directory.CreateDirectory(targetFilepath);

            var paths = new List<string>();

            foreach (var name in fileCodes.Keys)
            {
                string code = fileCodes[name];
                string filename = $"{name}.cs";
                string path = Path.Combine(targetFilepath, filename);

                await File.WriteAllTextAsync(path, code);

                paths.Add(path);
            }

            return paths;
        }
    }
}