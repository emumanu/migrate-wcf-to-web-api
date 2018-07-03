using System.Linq;

namespace MigrateWcfToWebApi.Core.Mappers
{
    internal static class MethodNamesMapper
    {
        public static string AddParametersToDefaultMethodName(string defaultMethodName, params string[] parameterNames)
        {
            string parameterNamesPart = parameterNames
                .Select(NamesMapper.ConvertFirstCharToUppercase)
                .Aggregate((name1, name2) => $"{name1}{name2}");

            string methodName = $"{defaultMethodName}By{parameterNamesPart}";

            return methodName;
        }

        public static string MapToMethodId(string methodName, string parametersFullString)
        {
            // create a unique identifier for any method. assumption all methods belong to same class
            // `parametersFullString` is expected to be in same format as provided from rosyln api's `<MethodDeclaratonSyntax>.ParameterList.Parameters.ToFullString()`
            string id = $"{methodName}({parametersFullString})";

            return id;
        }
    }
}