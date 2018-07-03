using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class ParametersGenerator
    {
        public static string FindParameterType(SeparatedSyntaxList<ParameterSyntax> parameters, string parameterName)
        {
            var type = parameters
                .Where(parameter =>
                {
                    var name = parameter
                        .Identifier
                        .ValueText;

                    var isMatch = name == parameterName;

                    return isMatch;
                })
                .Select(parameter =>
                {
                    var parameterType = parameter
                        .Type
                        .NormalizeWhitespace()
                        .ToFullString();

                    return parameterType;
                })
                .SingleOrDefault();

            return type;
        }

        public static int FindIndexBeforeFirstOptionalParam(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var lastIndex = parameters.Count;

            var firstOptionalParamIndex = parameters.IndexOf(param =>
            {
                var isOptionalParam = param.Default != null;

                return isOptionalParam;
            });
            var hasOptionalParam = firstOptionalParamIndex == -1;

            var index = hasOptionalParam
                ? lastIndex
                : firstOptionalParamIndex;

            return index;
        }
    }
}