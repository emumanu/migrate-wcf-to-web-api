using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.DataAccess;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.Models.Builders
{
    public static class WcfServiceClassInfoBuilder
    {
        public static Dictionary<string, Task<WcfServiceClassInfo>> Create(string wcfServiceDir)
        {
            var fileCodes = SourceCodeAccess.GetServiceFilesCode(wcfServiceDir);

            var wcfClasses = fileCodes
                .Select(async fileCode =>
                {
                    var code = (await fileCode).code;
                    var wcfClassName = await CodeParser.ParseClassName(code);

                    var info = new WcfServiceClassInfo
                    {
                        ControllerName = ServiceNamesMapper.MapToControllerName(wcfClassName),
                        ControllerNamespace = await GetControllerNamespace(code),
                        WcfServiceSourceCode = code,
                        WcfMethods = await CodeParser.ParseMethodNames(code),
                        IsAsmx = (await fileCode).isAsmx,
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

        private static async Task<string> GetControllerNamespace(string code)
        {
            // get api controllers' namespace

            var wcfNamespace = await CodeParser.ParseNamespace(code);

            var controllerNamespace = new[] {code, wcfNamespace}.Any(string.IsNullOrEmpty)
                ? ServiceNamesMapper.GetDefaultControllerNamespace()
                : ServiceNamesMapper.MapToControllersNamespace(wcfNamespace);

            return controllerNamespace;
        }
    }
}