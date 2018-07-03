using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class MultipleComplexTypesGenerator
    {
        public static async Task<ClassDeclarationSyntax> CreateClassFromComplexTypes(SeparatedSyntaxList<ParameterSyntax> wcfParameters, string className)
        {
            var complexParameters = FindMultipleComplexTypesOrOutKeyword(wcfParameters);
            if (!complexParameters.Any())
            {
                return default;
            }

            var classProperties = complexParameters.Select(parameter =>
                {
                    var propertyType = parameter.Type.ToFullString();

                    var parameterName = parameter.Identifier.ToFullString();
                    var propertyName = ComplexTypeNamesMapper.MapToComplexClassPropertyName(parameterName);

                    return $"public {propertyType} {propertyName} {{ get; set; }}";
                })
                .Aggregate((prop1, prop2) => $"{prop1}{Environment.NewLine}{prop2}");

            var classCode = $@"
        public class {className}
        {{
            {classProperties} 
        }}
";

            var root = await CodeParser.ConvertToCompilationUnit(classCode);

            var classDeclaration = root
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First();

            return classDeclaration;
        }

        public static SeparatedSyntaxList<ParameterSyntax> FindMultipleComplexTypesOrOutKeyword(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            // 'out' keywords are treated as complex types even if simple type
            var complexTypes = ComplexTypesGenerator.FindComplexTypes(parameters);

            // if at least one of the complex types is an 'out' keyword then return params
            if (OutKeywordGenerator.AnyHaveOutKeyword(complexTypes))
            {
                return complexTypes;
            }

            // only find parameters that are "complex" types if two or more exists
            // asp.net will automatically bind a single complex type parameter with the body of an http request
            // but not if more than one exist (an error will occur) so need find those and handle them
            const int minParameterCount = 2;

            if (parameters.Count < minParameterCount)
            {
                return default;
            }

            var parameterList = complexTypes.Count < minParameterCount
                ? default
                : complexTypes;

            return parameterList;
        }
    }
}