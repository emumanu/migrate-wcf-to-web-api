using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.DataAccess;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.Models.Builders
{
    public static class WcfClientClassInfoBuilder
    {
        public static Dictionary<string, Task<WcfClientClassInfo>> Create(string wcfClientDir, IDictionary<string, string> serviceGenCodes,
            Dictionary<string, Task<WcfServiceClassInfo>> wcfServiceClasses)
        {
            var fileCodes = SourceCodeAccess.GetClientFilesCode(wcfClientDir);

            var wcfClasses = fileCodes
                .Select(async fileCode =>
                {
                    var code = await fileCode;

                    var wcfClassName = await FindWcfClassName(code);

                    var info = new WcfClientClassInfo
                    {
                        ClientClassName = ClientNamesMapper.MapToClientClassName(wcfClassName),
                        ClientNamespace = await GetClientNamespace(code),
                        ClientBaseInterfaceName = ClientNamesMapper.MapToInterfaceName(wcfClassName),
                        WcfClientSourceCode = code,
                        ServiceGenCode = FindServiceGenCode(serviceGenCodes, wcfClassName),
                        WcfServiceClass = await FindWcfServiceClass(wcfServiceClasses, wcfClassName),
                    };

                    var pair = new
                    {
                        wcfClassName,
                        info,
                    };

                    return pair;
                })
                .ToDictionary(pair =>
                {
                    // avoid using task object as key for dictionary so wait to complete
                    var className = pair.Result.wcfClassName;

                    return className;
                }, async pair =>
                {
                    var info = (await pair).info;

                    return info;
                });

            return wcfClasses;
        }

        private static async Task<string> FindWcfClassName(string code)
        {
            // search by class name. if can't find then by interface name

            var className = await CodeParser.ParseClassName(code) ?? await CodeParser.ParseInterfaceName(code);
            var wcfClassName = ClientNamesMapper.MapToClassNameIfInterfaceName(className);

            return wcfClassName;
        }

        private static async Task<string> GetClientNamespace(string code)
        {
            // get clients' namespace

            var wcfNamespace = await CodeParser.ParseNamespace(code);

            var clientNamespace = new[] {code, wcfNamespace}.Any(string.IsNullOrEmpty)
                ? ClientNamesMapper.GetDefaultClientNamespace()
                : ClientNamesMapper.MapToClientNamespace(wcfNamespace);

            return clientNamespace;
        }

        private static string FindServiceGenCode(IDictionary<string, string> serviceGenCodes, string wcfClassName)
        {
            var controllerName = ServiceNamesMapper.MapToControllerName(wcfClassName);
            var serviceGenCode = serviceGenCodes.ContainsKey(controllerName)
                ? serviceGenCodes[controllerName]
                : "";

            return serviceGenCode;
        }

        private static async Task<WcfServiceClassInfo> FindWcfServiceClass(Dictionary<string, Task<WcfServiceClassInfo>> wcfServiceClasses, string wcfClassName)
        {
            var containsKey = wcfServiceClasses.ContainsKey(wcfClassName);
            var wcfServiceClass = containsKey
                ? await wcfServiceClasses[wcfClassName]
                : null;

            return wcfServiceClass;
        }
    }
}