using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeMethodsGenerator
    {
        public static async Task<string> CreateMethods(List<MethodDeclarationSyntax> wcfClientMethods, IEnumerable<MethodDeclarationSyntax> serviceGenMethods,
            List<MethodDeclarationSyntax> wcfServiceMethods)
        {
            var methods = await wcfClientMethods
                .Select(async wcfClientMethod =>
                {
                    var returnType = wcfClientMethod.ReturnType.ToFullString();
                    var methodName = wcfClientMethod.Identifier.ValueText;
                    var parameters = wcfClientMethod.ParameterList.ToFullString();
                    var serviceGenMethod = FindServiceGenMethod(wcfClientMethod, wcfClientMethods, serviceGenMethods);
                    var isActiveWcfMethod = serviceGenMethod != null;

                    var complexTypeClass = await ClientCodeMultipleComplexTypesGenerator.CreateComplexTypeClass(wcfClientMethod, methodName, isActiveWcfMethod);
                    var block = CreateMethodBlock(wcfClientMethod, serviceGenMethod, wcfServiceMethods, isActiveWcfMethod);
                    block = ClientCodeOutKeywordGenerator.TransformMethodBlockWithOutKeywords(block, wcfClientMethod, isActiveWcfMethod);

                    var method = $@"
{complexTypeClass}
public {returnType}{methodName}{parameters}
{block}
";
                    return method;
                })
                .Aggregate(async (method1, method2) => $"{await method1}\r\n{await method2}");

            return methods;
        }

        private static MethodDeclarationSyntax FindServiceGenMethod(MethodDeclarationSyntax wcfMethod,
            IEnumerable<MethodDeclarationSyntax> wcfMethods, IEnumerable<MethodDeclarationSyntax> serviceGenMethods)
        {
            var wcfMethodName = wcfMethod.Identifier.ValueText;
            var wcfParameters = wcfMethod.ParameterList.Parameters;

            // need to account for overload methods (duplicate)
            var duplicateMethodNamesMap = DuplicateMethodNamesGenerator.CreateMappings(wcfMethods);
            var serviceMethodName = DuplicateMethodNamesGenerator.TransformMethodNameIfDuplicate(duplicateMethodNamesMap, wcfMethodName, wcfParameters);

            var serviceGenMethod = serviceGenMethods.SingleOrDefault(serviceMethod =>
            {
                var name = serviceMethod.Identifier.ValueText;

                return name == serviceMethodName;
            });

            return serviceGenMethod;
        }


        private static string CreateMethodBlock(MethodDeclarationSyntax wcfClientMethod, MethodDeclarationSyntax serviceGenMethod, List<MethodDeclarationSyntax> wcfServiceMethods,
            bool isActiveWcfMethod)
        {
            if (!isActiveWcfMethod)
            {
                return "{ throw new NotImplementedException(); }";
            }

            var interfaceParameterNameMappings = ClientCodeInterfaceNamesGenerator.CreateInterfaceParameterNameMappings(wcfClientMethod, wcfServiceMethods);
            var requestUri = ClientCodeRequestUriGenerator.CreateRequestUri(serviceGenMethod);
            var clientRequest = ClientCodeClientRequestGenerator.CreateClientRequest(wcfClientMethod, serviceGenMethod, wcfServiceMethods);

            var block = $@"
{{
    {interfaceParameterNameMappings}
    {requestUri}
    {clientRequest}
}}
";

            return block;
        }
    }
}