using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Service
{
    internal static class ServiceCodeOutKeywordGenerator
    {
        public static ClassDeclarationSyntax TransformComplexClassWithOutKeyword(ClassDeclarationSyntax complexClass, SeparatedSyntaxList<ParameterSyntax> wcfParameters, string wcfReturnType)
        {
            if (!OutKeywordGenerator.AnyHaveOutKeyword(wcfParameters))
            {
                return complexClass;
            }

            var classProperties = complexClass.Members.OfType<PropertyDeclarationSyntax>();

            var outKeywordProperty = $"public {wcfReturnType} Result {{ get; set;}}";
            var outKeywordPropertyDeclaration = SyntaxFactory.ParseCompilationUnit(outKeywordProperty)
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .First();

            var newProperties = new List<PropertyDeclarationSyntax>();
            newProperties.AddRange(classProperties);
            newProperties.Add(outKeywordPropertyDeclaration);

            var newComplexClass = complexClass.WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(newProperties));

            return newComplexClass;
        }

        public static string TransformOutKeywordReturnType(string wcfReturnType, SeparatedSyntaxList<ParameterSyntax> wcfParameters, string wcfMethodName)
        {
            var isVoidReturnType = wcfReturnType == "void";

            if (!OutKeywordGenerator.AnyHaveOutKeyword(wcfParameters) || isVoidReturnType)
            {
                return wcfReturnType;
            }

            var complexVariableName = ComplexTypeNamesMapper.MapToServiceComplexClassType(wcfMethodName);

            var returnType = complexVariableName;

            return returnType;
        }

        public static BlockSyntax TransformBlockWithOutKeyword(BlockSyntax block, SeparatedSyntaxList<ParameterSyntax> wcfParameters,
            IEnumerable<SyntaxNodeOrToken> arguments, string methodName)
        {
            if (!OutKeywordGenerator.AnyHaveOutKeyword(wcfParameters))
            {
                return block;
            }

            block = AddStatementsForOutKeyword(block, arguments, wcfParameters, methodName);
            block = TransformReturnStatement(block, methodName);

            return block;
        }

        private static BlockSyntax AddStatementsForOutKeyword(BlockSyntax block, IEnumerable<SyntaxNodeOrToken> arguments, SeparatedSyntaxList<ParameterSyntax> wcfParameters, string wcfMethodName)
        {
            var argumentNames = GetArgumentNames(arguments);

            // add to start of method block
            var firstStatementIndex = 0;
            var startStatements = CreateStartStatements(argumentNames, wcfParameters);
            var statements = block.Statements.InsertRange(firstStatementIndex, startStatements);

            // add before end (i.e. before the return statement) of method block
            var lastStatementIndex = statements.Count - 1;
            var endStatements = CreateEndStatements(argumentNames, wcfMethodName);
            statements = statements.InsertRange(lastStatementIndex, endStatements);

            block = block.WithStatements(statements);

            return block;
        }

        private static BlockSyntax TransformReturnStatement(BlockSyntax block, string methodName)
        {
            var lastStatement = block.Statements.Last();

            if (!(lastStatement is ReturnStatementSyntax))
            {
                return block;
            }

            var complexVariableName = ComplexTypeNamesMapper.MapToComplexClassParameterName(methodName);
            var outKeywordReturnStatement = $"return Ok({complexVariableName});";
            var newStatements = block.Statements.Replace(lastStatement, SyntaxFactory.ParseStatement(outKeywordReturnStatement));

            var newBlock = block.WithStatements(newStatements);

            return newBlock;
        }

        private static List<string> GetArgumentNames(IEnumerable<SyntaxNodeOrToken> arguments)
        {
            var names = arguments
                .Where(argument =>
                {
                    var hasOutKeyword = OutKeywordGenerator.HasOutKeywordForArgument(argument);

                    return hasOutKeyword;
                })
                .Select(argument =>
                {
                    var argumentName = OutKeywordGenerator.RemoveOutKeyword(argument.ToFullString());

                    return argumentName;
                })
                .ToList();

            return names;
        }

        private static IEnumerable<StatementSyntax> CreateStartStatements(IEnumerable<string> argumentNames, SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            // arguments with 'out' keywords requires separate local variable declarations to be passed to wcf method
            // i.e. the complex class properties cannot be used as arguments with 'out' keyword...must use separate variable

            var statements = argumentNames
                .Select(argumentName =>
                {
                    var variableName = argumentName;
                    var variableType = ParametersGenerator.FindParameterType(wcfParameters, argumentName);

                    var statement = $"{variableType} {variableName};";
                    var statementDeclaration = SyntaxFactory.ParseStatement(statement);

                    return statementDeclaration;
                });

            return statements;
        }

        private static IEnumerable<StatementSyntax> CreateEndStatements(IEnumerable<string> argumentNames, string wcfMethodName)
        {
            var resultStatement = $"{ComplexTypeNamesMapper.MapToComplexTypeArgument(wcfMethodName, "Result")} = result;";

            var endStatements = argumentNames
                .Select(argumentName =>
                {
                    var complexTypeArgument = ComplexTypeNamesMapper.MapToComplexTypeArgument(wcfMethodName, argumentName);

                    var statement = $"{complexTypeArgument} = {argumentName};";
                    var statementDeclaration = SyntaxFactory.ParseStatement(statement);

                    return statementDeclaration;
                })
                .Prepend(SyntaxFactory.ParseStatement(resultStatement));

            return endStatements;
        }
    }
}