using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeMultipleComplexTypesGenerator
    {
        public static async Task<string> CreateComplexTypeClass(MethodDeclarationSyntax wcfMethod, string methodName, bool isActiveWcfMethod)
        {
            if (!isActiveWcfMethod)
            {
                return "";
            }

            var parameters = wcfMethod.ParameterList.Parameters;

            // add return type as class property if has 'out' keyword
            if (!OutKeywordGenerator.AnyHaveOutKeyword(parameters))
            {
                return "";
            }

            // create complex class
            var className = ComplexTypeNamesMapper.MapToClientComplexClassName(methodName);
            var complexClassDeclaration = await MultipleComplexTypesGenerator.CreateClassFromComplexTypes(parameters, className);

            // create return type property
            var wcfReturnType = wcfMethod.ReturnType.NormalizeWhitespace().ToFullString();
            var returnTypePropertyCode = $"public {wcfReturnType} Result {{ get; set;}}";
            var returnTypeProperty = (await CodeParser.ConvertToMemberDeclarations(returnTypePropertyCode)).Single();

            // add return type property to complex class
            var members = complexClassDeclaration.Members.Add(returnTypeProperty);
            complexClassDeclaration = complexClassDeclaration.WithMembers(members);
            var complexTypeClass = complexClassDeclaration?.NormalizeWhitespace().ToFullString();

            return complexTypeClass;
        }

        public static string CreateMultipleComplexTypesClassInstance(MethodDeclarationSyntax wcfClientMethod, string serviceComplexTypeParamName,
            IEnumerable<MethodDeclarationSyntax> wcfServiceMethods)
        {
            // handle if method has two or more (i.e. "multiple") complex types parameters

            var wcfServiceMethod = MethodsGenerator.FindWcfServiceMethod(wcfClientMethod, wcfServiceMethods);

            var wcfServiceParameters = wcfServiceMethod
                .ParameterList
                .Parameters;

            var multipleComplexParameters = MultipleComplexTypesGenerator.FindMultipleComplexTypesOrOutKeyword(wcfServiceParameters);

            // get the param names and use as anonymous class property names
            var properties = multipleComplexParameters.Any()
                ? multipleComplexParameters
                    .Select(parameter =>
                    {
                        var name = parameter.Identifier.ToFullString();

                        return name;
                    })
                    .Aggregate((name1, name2) => $"{name1},{name2}")
                : "";

            // create anonymous class instance with complex type properties
            var hasComplexTypeProperties = !string.IsNullOrEmpty(properties);
            var classInstance = hasComplexTypeProperties
                ? $@"var {serviceComplexTypeParamName} = new {{{properties}}};"
                : "";

            return classInstance;
        }
    }
}