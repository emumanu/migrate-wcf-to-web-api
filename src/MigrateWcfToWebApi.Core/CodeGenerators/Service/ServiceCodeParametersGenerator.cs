using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Service
{
    internal static class ServiceCodeParametersGenerator
    {
        public static SeparatedSyntaxList<ParameterSyntax> CreateParameters(SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            var parameters = AddAttributeToComplexTypeParameters(wcfParameters);
            parameters = RemoveDefaultValuesFromComplexTypeParameters(parameters);

            return parameters;
        }

        private static SeparatedSyntaxList<ParameterSyntax> AddAttributeToComplexTypeParameters(SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            // asp.net web api considers enums as 'simple types' and can be used in uri query string
            // however, unable to determine if a parameter is an enum type using roslyn parsing syntax api (instead this requires using rosyln's semantic model api to get type info)
            // hence the client needs to send enum values via the http request body instead of uri query string
            // therefore, enums will be treated as complex types but since can't identify them then must add explicit `FromBody` attribute to *all* complex types (even if unnecessary for classes)

            ParameterSyntax AddAttributeToParameter(string attributeName, ParameterSyntax parameter)
            {
                return parameter.WithAttributeLists(SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName))))
                    .WithOpenBracketToken(SyntaxFactory.Token(SyntaxKind.OpenBracketToken))
                    .WithCloseBracketToken(SyntaxFactory.Token(SyntaxKind.CloseBracketToken))));
            }

            // find complex type parameters
            var wcfComplexTypeParameters = ComplexTypesGenerator.FindComplexTypes(wcfParameters);

            // add attribute to complex type parameters
            var complexTypeParameters = wcfComplexTypeParameters
                .Select(parameter => AddAttributeToParameter("FromBody", parameter));

            var parameters = wcfParameters;

            // add modified complex type parameters back with other params
            foreach (var complexTypeParameter in complexTypeParameters)
            {
                var parameterName = complexTypeParameter.Identifier.ValueText;
                var oldParameter = parameters.Single(param => param.Identifier.ValueText == parameterName);

                parameters = parameters.Replace(oldParameter, complexTypeParameter);
            }

            return parameters;
        }

        private static SeparatedSyntaxList<ParameterSyntax> RemoveDefaultValuesFromComplexTypeParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            // asp.net web api does not support controller method parameters that are:
            //  1) reference types with default values set to 'null'
            //  2) enums with any default values
            // therefore remove all default values from complex type parameters
            // also the auto gen client files will include the default values so ok to remove them for enums
            // otherwise perhaps need to auto gen enum as a string (and parse it) or as a nullable type

            bool IsComplexTypeWithDefaultValue(ParameterSyntax parameter)
            {
                var param = new SeparatedSyntaxList<ParameterSyntax>().Add(parameter);
                var isComplexType = ComplexTypesGenerator.FindComplexTypes(param).Any();
                var hasDefaultValue = parameter.Default != null;

                return isComplexType && hasDefaultValue;
            }

            // split up complex type parameters into two lists: 1) has default values 2) all others
            var parametersGroupByDefaults = parameters.ToLookup(IsComplexTypeWithDefaultValue);
            var defaultValParameters = parametersGroupByDefaults[true].ToList();
            var otherParameters = parametersGroupByDefaults[false];

            if (!defaultValParameters.Any())
            {
                return parameters;
            }

            // remove default values
            var removedDefaultParameters = defaultValParameters
                .Select(parameter =>
                {
                    parameter = IsComplexTypeWithDefaultValue(parameter)
                        ? parameter.WithDefault(null)
                        : parameter;

                    return parameter;
                });

            // combine modified parameters no longer with default values back with other params
            var newParameters = new SeparatedSyntaxList<ParameterSyntax>();
            newParameters = newParameters.AddRange(otherParameters);
            var insertIndex = ParametersGenerator.FindIndexBeforeFirstOptionalParam(newParameters);
            newParameters = newParameters.InsertRange(insertIndex, removedDefaultParameters);

            return newParameters;
        }
    }
}