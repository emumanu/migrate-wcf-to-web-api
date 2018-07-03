using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class CodeParser
    {
        public static async Task<CompilationUnitSyntax> ConvertToCompilationUnit(string code)
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = (CompilationUnitSyntax) await tree.GetRootAsync();

            return root;
        }

        public static async Task<IEnumerable<MemberDeclarationSyntax>> ConvertToMemberDeclarations(string membersCode)
        {
            var members = (await ConvertToCompilationUnit(membersCode))
                .DescendantNodes()
                .OfType<MemberDeclarationSyntax>();

            return members;
        }

        public static async Task<string> ParseNamespace(string code)
        {
            var root = await ConvertToCompilationUnit(code);

            var @namespace = ParseNamespace(root);

            return @namespace;
        }

        public static string ParseNamespace(SyntaxNode root)
        {
            var @namespace = root
                .DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name
                .NormalizeWhitespace()
                .ToFullString();

            return @namespace;
        }

        public static async Task<List<string>> ParseMethodNames(string code)
        {
            var methodNames = (await ConvertToCompilationUnit(code))
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                .Select(method => method.Identifier.ToFullString())
                .ToList();

            return methodNames;
        }

        public static async Task<string> ParseClassName(string code)
        {
            var className = (await ConvertToCompilationUnit(code))
                .DescendantNodes()
                ?.OfType<ClassDeclarationSyntax>()
                .Where(method => method.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)))
                .Select(@class => @class.Identifier.NormalizeWhitespace().ToFullString())
                .FirstOrDefault();

            return className;
        }

        public static async Task<string> ParseInterfaceName(string code)
        {
            var interfaceName = (await ConvertToCompilationUnit(code))
                .DescendantNodes()
                ?.OfType<InterfaceDeclarationSyntax>()
                .Select(@interface => @interface.Identifier.NormalizeWhitespace().ToFullString())
                .FirstOrDefault();

            return interfaceName;
        }
    }
}