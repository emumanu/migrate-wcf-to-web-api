using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.CodeGenerators.Common;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Service
{
    internal static class ServiceCodeMethodsGenerator
    {
        public static async Task<IEnumerable<MemberDeclarationSyntax>> CreateMethodDeclarations(string controllerName, SyntaxNode wcfUnit,
            IEnumerable<string> wcfMethodNames, bool isAsmx)
        {
            // create new web api methods from filtered list of wcf methods
            var wcfMethods = wcfUnit
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(wcfMethod => wcfMethodNames.Contains(wcfMethod.Identifier.ValueText))
                .ToList();

            // create members including methods and nested classes
            var wcfClassName = wcfUnit
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First()
                .Identifier
                .ValueText;

            // build unique method name mappings if any methods (i.e. overload) have duplicate names
            var namesMap = DuplicateMethodNamesGenerator.CreateMappings(wcfMethods);

            var methodsTasks = wcfMethods
                .Select(async wcfMethod => await CreateMethods(controllerName, wcfMethod, wcfClassName, namesMap, isAsmx))
                .ToList();

            var methods = (await Task.WhenAll(methodsTasks))
                .SelectMany(method => method)
                .ToList();

            return methods;
        }

        private static async Task<IEnumerable<MemberDeclarationSyntax>> CreateMethods(string controllerName,
            MethodDeclarationSyntax wcfMethod, string wcfClassName, IDictionary<string, string> duplicateMethodNamesMap, bool isAsmx)
        {
            var members = new List<MemberDeclarationSyntax>();

            var wcfMethodName = wcfMethod.Identifier.ValueText;
            var wcfParameters = wcfMethod.ParameterList.Parameters;

            // create method & parameter names
            var methodName = DuplicateMethodNamesGenerator.TransformMethodNameIfDuplicate(duplicateMethodNamesMap, wcfMethodName, wcfParameters);
            var parameters = ServiceCodeParametersGenerator.CreateParameters(wcfParameters);
            parameters = ServiceCodeMultipleComplexTypesGenerator.TransformMultipleComplexTypeParameters(parameters, methodName);

            // create service type for method comment
            var serviceType = ServiceNamesMapper.GetServiceTypeName(isAsmx);

            // create http verb
            var httpVerb = CreateHttpVerb(wcfMethodName, parameters);

            // create route name
            var routeUriTemplate = ServiceNamesMapper.MapToRouteUriTemplate(controllerName, methodName);

            // create return type
            var wcfReturnType = wcfMethod
                .ReturnType
                .NormalizeWhitespace()
                .ToFullString();
            var returnType = ServiceCodeOutKeywordGenerator.TransformOutKeywordReturnType(wcfReturnType, wcfParameters, methodName);

            // create method block arguments
            var arguments = CreateWcfArguments(wcfParameters);
            arguments = ServiceCodeMultipleComplexTypesGenerator.TransformMultipleComplexTypeArguments(arguments, wcfParameters, methodName);

            // create method block
            var block = CreateMethodBlock(wcfClassName, wcfMethodName, arguments, returnType);
            block = ServiceCodeOutKeywordGenerator.TransformBlockWithOutKeyword(block, wcfParameters, arguments, methodName);

            // create new method
            var method = await CreateMethodDeclaration(methodName, httpVerb, routeUriTemplate, parameters, block, returnType, serviceType);

            // add nested class model if have multiple complex params
            var className = ComplexTypeNamesMapper.MapToServiceComplexClassType(methodName);
            var complexClass = await MultipleComplexTypesGenerator.CreateClassFromComplexTypes(wcfParameters, className);
            if (complexClass != null)
            {
                complexClass = ServiceCodeOutKeywordGenerator.TransformComplexClassWithOutKeyword(complexClass, wcfParameters, wcfReturnType);

                members.Add(complexClass);
            }

            // add new method
            members.Add(method);

            return members;
        }

        private static string CreateHttpVerb(string wcfMethodName, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            bool methodNameStartsWithGet = wcfMethodName.StartsWith("Get");
            bool hasAllSimpleTypesParameters = !ComplexTypesGenerator.FindComplexTypes(parameters).Any();
            bool isGetMethodWithSimpleTypes = methodNameStartsWithGet && hasAllSimpleTypesParameters;

            var httpVerb = isGetMethodWithSimpleTypes
                ? "HttpGet"
                : "HttpPost";

            return httpVerb;
        }

        private static async Task<MethodDeclarationSyntax> CreateMethodDeclaration(string methodName, string httpVerb,
            string routeUriTemplate, SeparatedSyntaxList<ParameterSyntax> parameters, BlockSyntax block, string responseType, string serviceType)
        {
            var methodSourceCode = $@"
/// <summary>
/// auto code generated using {serviceType} Service provider class
/// </summary>
[{httpVerb}, Route(""{routeUriTemplate}"")]
[ResponseType(typeof ({responseType}))]
public IHttpActionResult {methodName}()
{{
}}
";

            var root = await CodeParser.ConvertToCompilationUnit(methodSourceCode);

            var method = root
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

            method = method
                .WithParameterList(SyntaxFactory.ParameterList(parameters))
                .WithBody(block);

            return method;
        }

        private static List<SyntaxNodeOrToken> CreateWcfArguments(SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            var arguments = wcfParameters
                // add arguments using wcf method parameters
                .Select(parameter =>
                {
                    var argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Identifier));

                    // add 'out' keyword if param has it
                    var hasOutKeyword = OutKeywordGenerator.HasOutKeyword(parameter);
                    argument = hasOutKeyword
                        ? OutKeywordGenerator.AddOutKeywordToArgument(argument)
                        : argument;

                    return argument;
                })
                // add commas
                .SelectMany(param => new SyntaxNodeOrToken[]
                {
                    param,
                    SyntaxFactory.Token(SyntaxKind.CommaToken)
                })
                // remove last trailing comma
                .Take(wcfParameters.Count * 2 - 1)
                .ToList();

            return arguments;
        }

        private static BlockSyntax CreateMethodBlock(string wcfClassName, string wcfMethodName, IEnumerable<SyntaxNodeOrToken> arguments, string wcfReturnType)
        {
            bool isVoidReturnType = wcfReturnType == "void";
            var wcfClassVariableName = "result";

            var variableDeclaration = isVoidReturnType
                ? ""
                : $"var {wcfClassVariableName} = ";

            var argumentList = string.Join("", arguments.Select(argument => argument.ToFullString()));

            var returnResultsVariable = isVoidReturnType
                ? ""
                : wcfClassVariableName;

            var sourceCode = $@"
{{
    {variableDeclaration}{wcfClassName}.{wcfMethodName}({argumentList});

    return Ok({returnResultsVariable});
}}
";

            var block = (BlockSyntax) SyntaxFactory.ParseStatement(sourceCode);

            return block;
        }
    }
}