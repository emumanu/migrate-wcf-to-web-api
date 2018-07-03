using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class MethodsGenerator
    {
        public static MethodDeclarationSyntax FindWcfServiceMethod(MethodDeclarationSyntax wcfClientMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            // find matching service method using different approaches until finding it
            // however, this may potentially still not find a matching method if any of the parameter types use fully/partially qualified namespaces
            // may need to use c# roslyn semantic model or a more naive approach is to perhaps remove namespaces from the param types before comparing

            var serviceMethods = wcfServiceMethods.ToList();
            var serviceMethod = FindWcfServiceMethodByMethodId(wcfClientMethod, serviceMethods) ??
                                FindWcfServiceMethodByName(wcfClientMethod, serviceMethods);

            return serviceMethod;
        }

        private static MethodDeclarationSyntax FindWcfServiceMethodByMethodId(MethodDeclarationSyntax wcfClientMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            // find method by combo of method name + parameter names/types

            var clientMethodName = wcfClientMethod.Identifier.ValueText;
            var clientParameters = wcfClientMethod.ParameterList.Parameters;
            var clientMethodId = MethodNamesMapper.MapToMethodId(clientMethodName, clientParameters.ToFullString());

            var wcfServiceMethod = wcfServiceMethods
                .SingleOrDefault(serviceMethod =>
                {
                    var serviceMethodName = serviceMethod.Identifier.ValueText;
                    var serviceParams = serviceMethod.ParameterList.Parameters;
                    var serviceParameterId = MethodNamesMapper.MapToMethodId(serviceMethodName, serviceParams.ToFullString());

                    return serviceParameterId == clientMethodId;
                });

            return wcfServiceMethod;
        }

        private static MethodDeclarationSyntax FindWcfServiceMethodByName(MethodDeclarationSyntax wcfClientMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            // find method by name and param count

            var wcfServiceMethod = wcfServiceMethods
                .Where(serviceMethod =>
                {
                    var serviceParametersCount = serviceMethod.ParameterList.Parameters.Count;
                    var clientParametersCount = wcfClientMethod.ParameterList.Parameters.Count;

                    return serviceParametersCount == clientParametersCount;
                })
                .First(serviceMethod =>
                {
                    var serviceMethodName = serviceMethod.Identifier.ValueText;
                    var clientMethodName = wcfClientMethod.Identifier.ValueText;

                    return serviceMethodName == clientMethodName;
                });

            return wcfServiceMethod;
        }
    }
}