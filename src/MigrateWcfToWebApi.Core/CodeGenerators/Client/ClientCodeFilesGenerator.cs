using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Models;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    public static class ClientCodeFilesGenerator
    {
        public static async Task<IDictionary<string, string>> Run(IDictionary<string, Task<WcfClientClassInfo>> wcfClasses)
        {
            var codes = new Dictionary<string, string>();

            foreach (var wcfClassName in wcfClasses.Keys)
            {
                // wcf class info
                var wcfInfo = await wcfClasses[wcfClassName];

                var classNamespace = wcfInfo.ClientNamespace;
                var className = wcfInfo.ClientClassName;
                var baseInterfaceName = wcfInfo.ClientBaseInterfaceName;
                var wcfClientSourceCode = wcfInfo.WcfClientSourceCode;
                var serviceGenCode = wcfInfo.ServiceGenCode;
                var wcfServiceClass = wcfInfo.WcfServiceClass;

                // check if has wcf code or associated service gen code
                if (!HasSourceCode(className, wcfClientSourceCode, serviceGenCode, codes))
                {
                    continue;
                }

                // parse wcf & service file code
                var wcfClientUnit = await CodeParser.ConvertToCompilationUnit(wcfClientSourceCode);

                var hasWcfClientInterface = wcfClientUnit.DescendantNodes()
                    .OfType<InterfaceDeclarationSyntax>()
                    .Any();

                var wcfClientMethods = wcfClientUnit
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .ToList();

                var serviceGenUnit = await CodeParser.ConvertToCompilationUnit(serviceGenCode);
                var serviceGenMethods = serviceGenUnit
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>();

                var sourceCode = wcfServiceClass.WcfServiceSourceCode;
                var unitSyntax = await CodeParser.ConvertToCompilationUnit(sourceCode);
                var wcfServiceMethods = unitSyntax
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .ToList();

                // create header file comments
                var headerFileComment = CreateHeaderFileComment();

                // create using directives
                var usingDirectives = CreateUsingDirectives(wcfClientUnit, hasWcfClientInterface);

                // create base interface
                var baseInterface = CreateBaseInterface(baseInterfaceName, hasWcfClientInterface);

                // create methods
                var methods = await ClientCodeMethodsGenerator.CreateMethods(wcfClientMethods, serviceGenMethods, wcfServiceMethods);

                // create source code
                var sourcCode = CreateSourcCode(headerFileComment, usingDirectives, classNamespace, className, baseInterface, methods);

                // finalize & format generated code
                var root = await CodeParser.ConvertToCompilationUnit(sourcCode);

                var code = root
                    .NormalizeWhitespace()
                    .ToFullString();

                codes.Add(className, code);
            }

            return codes;
        }

        private static bool HasSourceCode(string className, string wcfClientSourceCode, string serviceGenCode, IDictionary<string, string> codes)
        {
            if (!new[] {wcfClientSourceCode, serviceGenCode}.Any(string.IsNullOrEmpty))
            {
                return true;
            }

            var reasonsMessages = string.IsNullOrEmpty(wcfClientSourceCode)
                ? "\n// - wcf client source code not found "
                : "";

            reasonsMessages += string.IsNullOrEmpty(serviceGenCode)
                ? "\n// - service auto generated code not found "
                : "";

            var errorMessage = $"// unable to generate client code for {className}: {reasonsMessages}";

            codes.Add(className, errorMessage);

            return false;
        }

        private static string CreateSourcCode(string headerFileComment, string usingDirectives, string classNamespace,
            string className, string baseInterface, string methods)
        {
            string code = $@"
{headerFileComment}

{usingDirectives}

namespace {classNamespace}
{{
    internal class {className}{baseInterface}
    {{
        private static HttpClient _httpClient;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {{TypeNameHandling = TypeNameHandling.Auto}};

        public {className}(string baseUrl)
        {{
            _httpClient = new HttpClient {{BaseAddress = new Uri(baseUrl)}};
        }}

{methods}

    }}
}}
";
            return code;
        }

        private static string CreateHeaderFileComment()
        {
            var codeGenDateTime = DateTime.Now;

            var comment = $@"
/******************************************************************************************************************************/
/* IMPORTANT:                                                                                                                 */
/* This file was auto-generated by the console app 'MigrateWcfToWebApi.exe' on {codeGenDateTime}.                          */
/* Avoid editing since it can be overwritten.                                                                                 */
/******************************************************************************************************************************/
";
            return comment;
        }

        private static string CreateBaseInterface(string baseInterfaceName, bool hasWcfInterface)
        {
            var baseInterface = hasWcfInterface
                ? $" : {baseInterfaceName}"
                : "";

            return baseInterface;
        }

        private static string CreateUsingDirectives(SyntaxNode wcfUnit, bool hasWcfInterface)
        {
            var includedUsings = new List<string>
            {
                // 'using directives' for http client requests/responses
                "System",
                "System.Net.Http",
                "System.Text",
                "System.Web",
                "Newtonsoft.Json",
            };

            var excludedUsings = new List<string>
            {
                // 'using directives' specific to wcf service
                "System.ServiceModel",
                "System.Web.Services",
            };

            // 'using directive' for referencing wcf client interface
            // if not an interface then exclude it
            var wcfNamespace = CodeParser.ParseNamespace(wcfUnit);
            if (hasWcfInterface)
            {
                includedUsings.Add(wcfNamespace);
            }
            else
            {
                excludedUsings.Add(wcfNamespace);
            }

            // 'using directives' referenced by wcf methods
            var wcfUsings = wcfUnit
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Where(usingDirective =>
                {
                    //   all 'usings directives' from wcf file are added regardless if actually used or not (a bit of a hack)
                    //   however, exclude any wcf service specific references
                    //   a better way is to use roslyn's semantic model/analysis to determine which 'usings' are truly used by the method parameters

                    var usingDirectiveName = usingDirective.Name.NormalizeWhitespace()
                        .ToFullString();
                    var includeUsing = !excludedUsings.Any(directive => usingDirectiveName.StartsWith(directive));

                    return includeUsing;
                })
                .Select(usingDirective => usingDirective.ToFullString());

            var usings = includedUsings
                .Select(usingName => $"using {usingName};\r\n")
                .Union(wcfUsings)
                .Aggregate((using1, using2) => $"{using1}{using2}");

            return usings;
        }
    }
}