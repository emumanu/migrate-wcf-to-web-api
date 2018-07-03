using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MigrateWcfToWebApi.Core.DataAccess
{
    internal static class SourceCodeAccess
    {
        public static IEnumerable<Task<(string code, bool isAsmx)>> GetServiceFilesCode(string wcfServiceDir)
        {
            var serviceFilesCode = Directory
                .EnumerateFiles(wcfServiceDir, "*.cs")
                .Select(async filename =>
                {
                    var code = await File.ReadAllTextAsync(filename);
                    var isAsmx = filename.EndsWith(".asmx.cs");

                    return (code, isAsmx);
                });

            return serviceFilesCode;
        }

        public static IEnumerable<Task<string>> GetClientFilesCode(string wcfClientDir)
        {
            var clientFilesCode = Directory
                .EnumerateFiles(wcfClientDir, "*.cs")
                .Select(async filename =>
                {
                    var code = await File.ReadAllTextAsync(filename);

                    return code;
                });

            return clientFilesCode;
        }
    }
}