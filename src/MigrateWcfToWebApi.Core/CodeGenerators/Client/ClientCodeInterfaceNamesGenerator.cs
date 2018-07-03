using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeInterfaceNamesGenerator
    {
        public static string CreateInterfaceParameterNameMappings(MethodDeclarationSyntax wcfClientMethod, IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            // sometimes interface methods' parameter names don't match names belonging to derived classes
            // therefore, find and map them

            var wcfServiceMethod = MethodsGenerator.FindWcfServiceMethod(wcfClientMethod, wcfServiceMethods);
            var mappings = BuildParameterMappings(wcfClientMethod, wcfServiceMethod);

            var mappingsCode = mappings.Any()
                ? mappings
                    .Select(map => $"var {map.serviceParameterName} = {map.clientParameterName};")
                    .Aggregate((map1, map2) => $"{map1}\n\r{map2}")
                : "";

            return mappingsCode;
        }

        private static List<(string serviceParameterName, string clientParameterName)> BuildParameterMappings(MethodDeclarationSyntax wcfClientMethod, MethodDeclarationSyntax wcfServiceMethod)
        {
            // find unmatched parameter names

            var serviceParameters = wcfServiceMethod.ParameterList.Parameters;
            var clientParameters = wcfClientMethod.ParameterList.Parameters;

            var serviceParametersCount = serviceParameters.Count;
            var clientParametersCount = clientParameters.Count;

            var hasSameCounts = serviceParametersCount == clientParametersCount;
            if (!hasSameCounts)
            {
                return new List<(string serviceParameterName, string clientParameterName)>();
            }

            var mappings = Enumerable
                .Range(0, serviceParametersCount)
                .Select(index =>
                {
                    var parameters =
                        new
                        {
                            clientName = clientParameters[index].Identifier.ValueText,
                            serviceName = serviceParameters[index].Identifier.ValueText,
                        };

                    return parameters;
                })
                .Where(parameters => parameters.serviceName != parameters.clientName)
                .Select(parameters => (parameters.serviceName, parameters.clientName))
                .ToList();

            return mappings;
        }
    }
}