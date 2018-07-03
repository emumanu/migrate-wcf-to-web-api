using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class ComplexTypesGenerator
    {
        public static SeparatedSyntaxList<ParameterSyntax> FindComplexTypes(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var complexTypes = FindTypes(parameters, isSimpleTypesMode: false);

            return complexTypes;
        }

        public static SeparatedSyntaxList<ParameterSyntax> FindSimpleTypes(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var simpleTypes = FindTypes(parameters, isSimpleTypesMode: true);

            return simpleTypes;
        }

        private static SeparatedSyntaxList<ParameterSyntax> FindTypes(SeparatedSyntaxList<ParameterSyntax> parameters, bool isSimpleTypesMode)
        {
            // per ms docs, "...Simple types include the .NET primitive types (int, bool, double, and so forth), plus TimeSpan, DateTime, Guid, decimal, and string,..."
            // https://docs.microsoft.com/en-us/aspnet/web-api/overview/formats-and-model-binding/parameter-binding-in-aspnet-web-api
            // below is not the complete list but the most common ones we use
            var simpeTypes = new HashSet<string>
            {
                "int",
                "string",
                "bool",
                "DateTime",
                "int?",
                "bool?",
                "DateTime?",
            };

            var typeParameters = parameters
                .Where(parameter =>
                {
                    string parameterType = SyntaxNodeExtensions.NormalizeWhitespace<TypeSyntax>(parameter
                            .Type)
                        .ToFullString();

                    var hasOutKeyword = OutKeywordGenerator.HasOutKeyword(parameter);

                    // 'out' keywords will be treated as complex types even if simple type
                    var hasSimpleTypes = simpeTypes.Contains(parameterType) && !hasOutKeyword;

                    return hasSimpleTypes == isSimpleTypesMode;
                })
                .ToList();

            var typeParameterList = new SeparatedSyntaxList<ParameterSyntax>().AddRange(typeParameters);

            return typeParameterList;
        }
    }
}