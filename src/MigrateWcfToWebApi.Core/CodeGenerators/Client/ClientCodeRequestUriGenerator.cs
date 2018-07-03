using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeRequestUriGenerator
    {
        public static string CreateRequestUri(MethodDeclarationSyntax serviceGenMethod)
        {
            var routeUri = CreateRouteUri(serviceGenMethod);
            var queryString = CreateRequestQueryString(serviceGenMethod);
            var stringLiteral = string.IsNullOrEmpty(queryString)
                ? ""
                : "$";

            var requestUri = $@"
var requestUri = {stringLiteral}""{routeUri}{queryString}"";
";

            return requestUri;
        }

        private static string CreateRouteUri(MethodDeclarationSyntax serviceGenMethod)
        {
            var routeUri = serviceGenMethod
                ?.AttributeLists
                .Select(attribute => attribute.Attributes.First(a => a.Name.ToFullString() == "Route"))
                .First()
                .ArgumentList
                .Arguments
                .ToFullString()
                .Replace("\"", "");

            return routeUri;
        }

        private static string CreateRequestQueryString(MethodDeclarationSyntax serviceGenMethod)
        {
            string queryString;

            var simpleTypes = ComplexTypesGenerator.FindSimpleTypes(serviceGenMethod.ParameterList.Parameters);
            if (!simpleTypes.Any())
            {
                queryString = "";

                return queryString;
            }

            var queryStringWithSimpleTypes = simpleTypes
                .Select(parameter =>
                {
                    var parameterName = parameter.Identifier.NormalizeWhitespace().ToFullString();
                    var parameterType = parameter.Type.NormalizeWhitespace().ToFullString();
                    var parameterValue = CreateParameterValue(parameterName, parameterType);

                    var queryStringParam = $"{parameterName}={{{parameterValue}}}";

                    return queryStringParam;
                })
                .Aggregate((parameter1, parameter2) => $"{parameter1}&{parameter2}");

            queryString = $"?{queryStringWithSimpleTypes}";

            return queryString;
        }

        private static string CreateParameterValue(string parameterName, string parameterType)
        {
            var parameterValue = parameterType == "string"
                ? $"HttpUtility.UrlEncode({parameterName})"
                : parameterName;

            return parameterValue;
        }
    }
}