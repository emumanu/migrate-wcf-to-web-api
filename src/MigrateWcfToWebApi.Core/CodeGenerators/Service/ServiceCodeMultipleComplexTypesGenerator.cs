using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Service
{
    internal static class ServiceCodeMultipleComplexTypesGenerator
    {
        public static SeparatedSyntaxList<ParameterSyntax> TransformMultipleComplexTypeParameters(SeparatedSyntaxList<ParameterSyntax> originalParameters, string methodName)
        {
            // handle if parameters have more two or more (i.e. multiple) complex types

            var complexParameters = MultipleComplexTypesGenerator.FindMultipleComplexTypesOrOutKeyword(originalParameters);
            if (!complexParameters.Any())
            {
                return originalParameters;
            }

            var parameters = new SeparatedSyntaxList<ParameterSyntax>();

            // remove all complex types from parameters
            var complexNames = complexParameters
                .Select(complex => complex.Identifier.ToFullString());

            var filteredParameters = originalParameters
                .Where(parameter =>
                {
                    var name = parameter.Identifier.ToFullString();

                    return complexNames.All(complexName => complexName != name);
                });

            parameters = parameters.AddRange(filteredParameters);

            // add single complex class type parameter
            var complexType = ComplexTypeNamesMapper.MapToServiceComplexClassType(methodName);
            var complexParameterName = ComplexTypeNamesMapper.MapToComplexClassParameterName(methodName);

            var complexTypeClass = SyntaxFactory.Parameter(SyntaxFactory.Identifier(complexParameterName))
                .WithType(SyntaxFactory.IdentifierName(complexType));

            // insert complex type before any optional parameters with default values
            // otherwise insert at the end
            var insertIndex = ParametersGenerator.FindIndexBeforeFirstOptionalParam(parameters);
            parameters = parameters.Insert(insertIndex, complexTypeClass);

            return parameters;
        }

        public static List<SyntaxNodeOrToken> TransformMultipleComplexTypeArguments(List<SyntaxNodeOrToken> arguments,
            SeparatedSyntaxList<ParameterSyntax> wcfParameters, string wcfMethodName)
        {
            var complexParameters = MultipleComplexTypesGenerator.FindMultipleComplexTypesOrOutKeyword(wcfParameters);
            if (!complexParameters.Any())
            {
                return arguments;
            }

            // replace all complex type arguments with complex type class properties
            // if any arguments already have 'out' keyword then will be ignored
            var newArguments = arguments
                .Select(nodeOrToken =>
                {
                    var argument = (ArgumentSyntax) nodeOrToken;
                    var isArgument = argument != null;

                    if (!isArgument)
                    {
                        return nodeOrToken;
                    }

                    var argumentName = argument
                        .NormalizeWhitespace()
                        .ToFullString();

                    var isComplexArgument = complexParameters
                        .Select(parameter => parameter.Identifier.ValueText)
                        .Contains(argumentName);

                    if (!isComplexArgument)
                    {
                        return argument;
                    }

                    var complexTypeArgumentName = ComplexTypeNamesMapper.MapToComplexTypeArgument(wcfMethodName, argumentName);
                    var complexTypeArgument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(complexTypeArgumentName));

                    return complexTypeArgument;
                })
                .ToList();

            return newArguments;
        }
    }
}