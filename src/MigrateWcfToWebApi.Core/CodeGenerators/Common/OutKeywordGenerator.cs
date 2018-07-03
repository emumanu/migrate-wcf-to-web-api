using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharpExtensions;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class OutKeywordGenerator
    {
        public static bool HasOutKeyword(ParameterSyntax parameter)
        {
            bool hasOutKeyword = parameter
                .Modifiers
                .Any(modifier => CSharpExtensions.IsKind((SyntaxToken) modifier, SyntaxKind.OutKeyword));

            return hasOutKeyword;
        }

        public static bool HasOutKeywordForArgument(SyntaxNodeOrToken nodeOrToken)
        {
            var isArgument = nodeOrToken.IsKind(SyntaxKind.Argument);
            var hasOutKeyword = isArgument && ((ArgumentSyntax) nodeOrToken).RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword);

            return hasOutKeyword;
        }

        public static bool AnyHaveOutKeyword(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            bool hasOutKeyword = parameters.Any(HasOutKeyword);

            return hasOutKeyword;
        }

        public static string RemoveOutKeyword(string argumentFullstring)
        {
            return argumentFullstring.Replace("out ", "");
        }

        public static ArgumentSyntax AddOutKeywordToArgument(ArgumentSyntax argument)
        {
            var newArgument = argument?
                .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                .NormalizeWhitespace();

            return newArgument;
        }
    }
}