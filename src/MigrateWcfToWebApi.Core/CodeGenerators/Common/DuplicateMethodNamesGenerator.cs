using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MigrateWcfToWebApi.Core.Mappers;

namespace MigrateWcfToWebApi.Core.CodeGenerators.Common
{
    internal static class DuplicateMethodNamesGenerator
    {
        public static Dictionary<string, string> CreateMappings(IEnumerable<MethodDeclarationSyntax> wcfMethods)
        {
            // maps method IDs for duplicate (i.e. overload) methods with unique method names
            var namesMap = new Dictionary<string, string>();

            var duplicateMethodGroups = FindDuplicateMethodGroupsByName(wcfMethods);
            if (!duplicateMethodGroups.Any())
            {
                return namesMap;
            }

            foreach (var duplicateMethods in duplicateMethodGroups)
            {
                var maxParametersCount = duplicateMethods.Max(d => d.ParameterList.Parameters.Count);
                var parametersIndexes = Enumerable.Range(0, maxParametersCount + 1);
                var unavailableMethodNames = new HashSet<string>();
                var allParameterNamesCounts = GetAllParameterNamesCounts(duplicateMethods);

                // go through each duplicate named method starting with the ones with the lowest parameters usage count
                // and then add a unique name to each method based on its parameters as compared against the usage count of all others
                // ideally try to keep the method names as short as possible
                foreach (var parametersIndex in parametersIndexes)
                {
                    var filteredByParamCountMethods = duplicateMethods.Where(method => method.ParameterList.Parameters.Count == parametersIndex);

                    foreach (var method in filteredByParamCountMethods)
                    {
                        var methodName = method.Identifier.ToFullString();
                        var defaultMethodName = methodName;

                        var parameters = method.ParameterList.Parameters;
                        var parameterNames = GetParameterNames(parameters);
                        var methodId = MethodNamesMapper.MapToMethodId(methodName, parameters.ToFullString());

                        // run through each method name assignment rule
                        if (AssignDefaultNameToFirstMethod(parameterNames, parametersIndex, defaultMethodName, unavailableMethodNames, namesMap, methodId))
                        {
                            continue;
                        }

                        if (AssignMethodNameUsingUniqueParameter(parameterNames, allParameterNamesCounts, defaultMethodName, unavailableMethodNames, namesMap, methodId))
                        {
                            continue;
                        }

                        AssignMethodNameUsingAnyParameters(parameterNames, allParameterNamesCounts, defaultMethodName, unavailableMethodNames, namesMap, methodId);
                    }
                }
            }

            return namesMap;
        }

        public static string TransformMethodNameIfDuplicate(IDictionary<string, string> duplicateMethodNamesMap, string wcfMethodName, SeparatedSyntaxList<ParameterSyntax> wcfParameters)
        {
            // replace method name with unique method name if its a duplicate (i.e. overload)
            var methodId = MethodNamesMapper.MapToMethodId(wcfMethodName, wcfParameters.ToFullString());

            var modifiedMethodName = duplicateMethodNamesMap.ContainsKey(methodId)
                ? duplicateMethodNamesMap[methodId]
                : wcfMethodName;

            return modifiedMethodName;
        }

        private static List<IGrouping<string, MethodDeclarationSyntax>> FindDuplicateMethodGroupsByName(IEnumerable<MethodDeclarationSyntax> methods)
        {
            const int minimunDuplicateCount = 2;

            var duplicates = methods
                .GroupBy(method =>
                    {
                        string methodName = method.Identifier.ToFullString();

                        return methodName;
                    },
                    method => method)
                .Where(d => d.Count() >= minimunDuplicateCount)
                .ToList();

            return duplicates;
        }

        private static IDictionary<string, int> GetAllParameterNamesCounts(IEnumerable<MethodDeclarationSyntax> duplicateMethods)
        {
            var namesCounts = duplicateMethods
                .SelectMany(method => method
                    .ParameterList
                    .Parameters
                    .Select(parameter =>
                    {
                        string name = parameter.Identifier.ToFullString();

                        return name;
                    }))
                .GroupBy(name => name)
                .ToDictionary(name => name.Key, name => name.Count());

            return namesCounts;
        }

        private static List<string> GetParameterNames(SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            var names = parameters
                .Select(parameter =>
                {
                    string name = parameter.Identifier.ToFullString();

                    return name;
                })
                .ToList();

            return names;
        }

        private static bool AssignDefaultNameToFirstMethod(IEnumerable<string> parameterNames, int parametersCount, string defaultMethodName,
            ISet<string> unavailableMethodNames, IDictionary<string, string> methodNamesMap, string methodId)
        {
            // assign default method name (without parameters) to first duplicate method
            // the rest of the duplicate methods will have names with parameters in its name
            bool isFirstDuplicateMethod = parametersCount <= 1;
            if (!isFirstDuplicateMethod || unavailableMethodNames.Contains(defaultMethodName))
            {
                return false;
            }

            unavailableMethodNames.Add(defaultMethodName);
            methodNamesMap.Add(methodId, defaultMethodName);

            // if current method has a parameter then the modified method name that includes it as part of the name
            // should no longer be available for later assignment
            var currentParameterName = parameterNames.SingleOrDefault();

            if (string.IsNullOrEmpty(currentParameterName))
            {
                return true;
            }

            var modifiedName = MethodNamesMapper.AddParametersToDefaultMethodName(defaultMethodName, currentParameterName);
            unavailableMethodNames.Add(modifiedName);

            return true;
        }

        private static bool AssignMethodNameUsingUniqueParameter(IEnumerable<string> parameterNames, IDictionary<string, int> allParameterNamesCounts,
            string defaultMethodName, ISet<string> unavailableMethodNames, IDictionary<string, string> methodNamesMap, string methodId)
        {
            // filter by unique method parameter names
            var uniqueParameterNames = allParameterNamesCounts
                .Where(p => p.Value == 1)
                .Select(p => p.Key);

            // find first unique parameter within current parameters
            string firstUniqueParameter = parameterNames
                .Intersect(uniqueParameterNames)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(firstUniqueParameter))
            {
                return false;
            }

            // use first unique parameter as part of method name
            var modifiedName = MethodNamesMapper.AddParametersToDefaultMethodName(defaultMethodName, firstUniqueParameter);

            unavailableMethodNames.Add(modifiedName);
            methodNamesMap.Add(methodId, modifiedName);

            return true;
        }

        private static void AssignMethodNameUsingAnyParameters(List<string> parameterNames, IDictionary<string, int> allParameterNamesCounts,
            string defaultMethodName, ISet<string> unavailableMethodNames, IDictionary<string, string> methodNamesMap, string methodId)
        {
            // assign method name with first available combo of one or more params based on low usage counts
            // if initial combos are not available then eventually all parameters will be used as part of the name (which is guaranteed to be unique)

            // sort param names by lowest usage counts to increase probability of getting a shorter method name with fewer params
            // and decrease long names for methods with a lot of params
            var sortedParameterNames = parameterNames
                .OrderBy(name => allParameterNamesCounts[name])
                .ToList();

            var parameterCounts = Enumerable.Range(1, parameterNames.Count);

            foreach (var parameterCount in parameterCounts)
            {
                var filteredNames = sortedParameterNames
                    .Take(parameterCount)
                    .ToArray();

                var modifiedName = MethodNamesMapper.AddParametersToDefaultMethodName(defaultMethodName, filteredNames);

                if (unavailableMethodNames.Contains(modifiedName))
                {
                    continue;
                }

                unavailableMethodNames.Add(modifiedName);
                methodNamesMap.Add(methodId, modifiedName);

                return;
            }
        }
    }
}