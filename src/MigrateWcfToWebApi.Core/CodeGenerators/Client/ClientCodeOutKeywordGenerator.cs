using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Client
{
    internal static class ClientCodeOutKeywordGenerator
    {
        public static string TransformMethodBlockWithOutKeywords(string block, MethodDeclarationSyntax wcfMethod, bool isActiveWcfMethod)
        {
            if (!isActiveWcfMethod)
            {
                return block;
            }

            var wcfParameters = wcfMethod
                .ParameterList
                .Parameters;

            var hasOutKeyword = OutKeywordGenerator.AnyHaveOutKeyword(wcfParameters);
            if (!hasOutKeyword)
            {
                return block;
            }

            var blockDeclaration = (BlockSyntax) SyntaxFactory.ParseStatement($"{block}");
            var parameterNames = GetParameterNamesWithOutKeywords(wcfParameters);

            // add to start of method block
            var firstStatementIndex = 0;
            var startStatements = CreateStartStatements(parameterNames, wcfParameters);
            var statements = blockDeclaration.Statements.InsertRange(firstStatementIndex, startStatements);

            // add before end (i.e. before the return statement) of method block
            var oldStatement = FindStatementToReplace(statements);
            var endStatements = CreateEndStatements(parameterNames, wcfParameters, wcfMethod);
            statements = statements.ReplaceRange(oldStatement, endStatements);

            // finialize block
            blockDeclaration = blockDeclaration.WithStatements(statements);
            block = blockDeclaration.ToFullString();

            return block;
        }

        private static List<string> GetParameterNamesWithOutKeywords(IEnumerable<ParameterSyntax> parameters)
        {
            var names = parameters
                .Where(parameter =>
                {
                    var hasOutKeyword = OutKeywordGenerator.HasOutKeyword(parameter);

                    return hasOutKeyword;
                })
                .Select(parameter =>
                {
                    var parameterName = parameter.Identifier.ValueText;

                    return parameterName;
                })
                .ToList();

            return names;
        }

        private static IEnumerable<StatementSyntax> CreateStartStatements(IEnumerable<string> parameterNames,
            SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            var statements = parameterNames
                .Select(parameterName =>
                {
                    var variableName = parameterName;
                    var variableType = ParametersGenerator.FindParameterType(wcfParameters, parameterName);

                    var statement = $"{variableName} = default({variableType});";
                    var statementDeclaration = SyntaxFactory.ParseStatement(statement);

                    return statementDeclaration;
                });

            return statements;
        }

        private static IEnumerable<StatementSyntax> CreateEndStatements(IEnumerable<string> parameterNames, SeparatedSyntaxList<ParameterSyntax> wcfParameters,
            MethodDeclarationSyntax wcfMethod)
        {
            string CreatePropertyStatement(string variableName, bool isInitialized = false)
            {
                var initializeType = isInitialized ? "var " : "";
                var propertyName = ComplexTypeNamesMapper.MapToComplexClassPropertyName(variableName);

                var statement = $"{initializeType}{variableName} = deserializeObject.{propertyName};";

                return statement;
            }

            var methodName = wcfMethod.Identifier.ValueText;
            var complexClassType = ComplexTypeNamesMapper.MapToClientComplexClassName(methodName);

            var jsonDeserializeStatements = new List<string>
            {
                $"var deserializeObject = JsonConvert.DeserializeObject<{complexClassType}>(jsonResponse, _jsonSerializerSettings);",
                CreatePropertyStatement("result", isInitialized: true),
            };

            var propertyStatements = wcfParameters
                .Where(parameter =>
                {
                    var parameterName = parameter.Identifier.ValueText;
                    var hasOutKeyword = parameterNames.Contains(parameterName);

                    return hasOutKeyword;
                })
                .Select(parameter =>
                {
                    var parameterName = parameter.Identifier.ValueText;
                    var statement = CreatePropertyStatement(parameterName);

                    return statement;
                });

            var endStatements = jsonDeserializeStatements
                .Union(propertyStatements)
                .Select(statement =>
                {
                    var statementDeclaration = SyntaxFactory.ParseStatement(statement);

                    return statementDeclaration;
                });

            return endStatements;
        }

        private static StatementSyntax FindStatementToReplace(SyntaxList<StatementSyntax> statements)
        {
            var statementToReplace = statements
                .SingleOrDefault(statement => statement
                    .NormalizeWhitespace()
                    .ToFullString()
                    .StartsWith("var result = JsonConvert.DeserializeObject"));

            return statementToReplace;
        }
    }
}