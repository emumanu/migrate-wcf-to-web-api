using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Models;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Service
{
    /// <summary>
    /// auto generates files for web api controllers/action methods using wcf service code
    /// </summary>
    /// <remarks>some of the rosyln compiler code was modified based on snippets generated from http://roslynquoter.azurewebsites.net/ </remarks>
    public static class ServiceCodeFilesGenerator
    {
        public static async Task<IDictionary<string, string>> Run(IDictionary<string, Task<WcfServiceClassInfo>> wcfClasses)
        {
            var codes = new Dictionary<string, string>();

            // create service code files for web api controllers
            foreach (string wcfClassName in wcfClasses.Keys)
            {
                // wcf class info
                var wcfInfo = await wcfClasses[wcfClassName];

                var controllerName = wcfInfo.ControllerName;
                var controllerNamespace = wcfInfo.ControllerNamespace;
                var wcfServiceSourceCode = wcfInfo.WcfServiceSourceCode;
                var wcfMethods = wcfInfo.WcfMethods;
                var isAsmx = wcfInfo.IsAsmx;

                // check if has wcf code
                if (string.IsNullOrEmpty(wcfServiceSourceCode))
                {
                    codes.Add(controllerName, wcfServiceSourceCode);

                    continue;
                }

                // parse wcf code
                var wcfUnit = await CodeParser.ConvertToCompilationUnit(wcfServiceSourceCode);

                // initialize web api code gen
                var unit = SyntaxFactory.CompilationUnit();

                // create using directives for api controller
                var apiUsings = CreateApiControllerUsings();

                // create file header comments
                var fileHeaderComments = CreateFileHeaderComments();
                apiUsings[0] = apiUsings[0].WithUsingKeyword(fileHeaderComments);

                // create using directives for method parameters
                //   all 'usings directives' from wcf file are added regardless if actually used or not (a bit of a hack)
                //   a better way is to use roslyn's semantic model/analysis to determine which 'usings' are truly used by the method parameters
                var wcfUsings = wcfUnit.Usings;

                // add all usings & header comments
                var usingDirectives = BuildUsingDirectives(apiUsings, wcfUsings);
                unit = unit.AddUsings(usingDirectives.ToArray());

                // create namespace
                var namespaceDeclaration = CreateNamespaceDeclaration(controllerNamespace);

                // create field for wcf class instance
                var fieldDeclaration = CreateFieldDeclaration(wcfClassName);

                // create class
                var classDeclaration = CreateClassDeclaration(controllerName);
                classDeclaration = classDeclaration.AddMembers(fieldDeclaration);

                // create methods
                var methodDeclarations = await ServiceCodeMethodsGenerator.CreateMethodDeclarations(controllerName, wcfUnit, wcfMethods, isAsmx);
                classDeclaration = classDeclaration.AddMembers(methodDeclarations.ToArray());

                // finalize & format generated code
                namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);
                unit = unit.AddMembers(namespaceDeclaration);

                var code = unit
                    .NormalizeWhitespace()
                    .ToFullString();

                codes.Add(controllerName, code);
            }

            return codes;
        }

        private static SyntaxToken CreateFileHeaderComments()
        {
            var codeGenDateTime = DateTime.Now;

            var comments = SyntaxFactory.Token(
                SyntaxFactory.TriviaList(
                    new[]
                    {
                        SyntaxFactory.Comment("/*********************************************************************************************************************************/"),
                        SyntaxFactory.Comment("/* IMPORTANT:                                                                                                                    */"),
                        SyntaxFactory.Comment($"/* This file was auto-generated by the console app \'MigrateWcfToWebApi.exe\' on {codeGenDateTime}.                             */"),
                        SyntaxFactory.Comment("/* Avoid editing since it can be overwritten.                                                                                    */"),
                        SyntaxFactory.Comment("/*********************************************************************************************************************************/"),
                        SyntaxFactory.LineFeed,
                        SyntaxFactory.LineFeed,
                    }),
                SyntaxKind.UsingKeyword,
                SyntaxFactory.TriviaList(SyntaxFactory.Space));

            return comments;
        }

        private static List<UsingDirectiveSyntax> CreateApiControllerUsings()
        {
            var usingNames = new[]
            {
                "System.Web.Http",
                "System.Web.Http.Description",
            };

            var usingDirectiveSyntaxs = usingNames
                .Select(name => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name)))
                .ToList();

            return usingDirectiveSyntaxs;
        }

        private static SyntaxList<UsingDirectiveSyntax> BuildUsingDirectives(IEnumerable<UsingDirectiveSyntax> apiUsings, SyntaxList<UsingDirectiveSyntax> wcfUsings)
        {
            var usingDirectives = new SyntaxList<UsingDirectiveSyntax>();
            usingDirectives = usingDirectives.AddRange(apiUsings);
            usingDirectives = usingDirectives.AddRange(wcfUsings);

            return usingDirectives;
        }

        private static NamespaceDeclarationSyntax CreateNamespaceDeclaration(string controllerNamespace)
        {
            var name = SyntaxFactory.ParseName(controllerNamespace);
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(name);

            return namespaceDeclaration;
        }

        private static ClassDeclarationSyntax CreateClassDeclaration(string controllerName)
        {
            // initialize class
            var declaration = SyntaxFactory.ClassDeclaration(controllerName);

            // add modifiers to class
            var publicKeyword = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            declaration = declaration.AddModifiers(publicKeyword);

            // add base class
            var baseTypeName = SyntaxFactory.ParseTypeName("ApiController");
            var baseType = SyntaxFactory.SimpleBaseType(baseTypeName);
            declaration = declaration.AddBaseListTypes(baseType);

            return declaration;
        }

        private static FieldDeclarationSyntax CreateFieldDeclaration(string wcfClassName)
        {
            var className = SyntaxFactory.IdentifierName(wcfClassName);
            var variableName = SyntaxFactory.Identifier(wcfClassName);

            var variable = SyntaxFactory.VariableDeclaration(className)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(variableName)
                    .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.ObjectCreationExpression(className)
                        .WithArgumentList(SyntaxFactory.ArgumentList())))));

            var fieldDeclaration = SyntaxFactory.FieldDeclaration(variable)
                .WithModifiers(SyntaxFactory.TokenList(new[]
                {
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                    SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                    SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
                }));

            return fieldDeclaration;
        }
    }
}