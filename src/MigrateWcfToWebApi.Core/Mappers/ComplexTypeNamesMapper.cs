using System;

namespace MigrateWcfToWebApi.Core.Mappers
{
    internal static class ComplexTypeNamesMapper
    {
        public static string MapToComplexTypeArgument(string wcfMethodName, string oldArgumentName)
        {
            string classParameterName = MapToComplexClassParameterName(wcfMethodName);
            string classPropertyName = MapToComplexClassPropertyName(oldArgumentName);

            string complexTypeArgument = $"{classParameterName}.{classPropertyName}";

            return complexTypeArgument;
        }

        public static string MapToComplexClassPropertyName(string parameterName)
        {
            string complexPropertyName = NamesMapper.ConvertFirstCharToUppercase(parameterName);

            return complexPropertyName;
        }

        public static string MapToServiceComplexClassType(string methodName)
        {
            return $"{methodName}Params";
        }

        public static string MapToClientComplexClassName(string methodName)
        {
            return $"{methodName}Response";
        }

        public static string MapToComplexClassParameterName(string methodName)
        {
            string ConvertFirstCharToLowercase(string s)
            {
                // https://stackoverflow.com/a/3565041/4872
                return Char.ToLowerInvariant(s[0]) + s.Substring(1);
            }

            string complexType = MapToServiceComplexClassType(methodName);
            string complexParameterName = ConvertFirstCharToLowercase(complexType);

            return complexParameterName;
        }
    }
}