using System;
using System.Linq;

namespace MigrateWcfToWebApi.Core.Mappers
{
    internal static class ServiceNamesMapper
    {
        public static string MapToControllerName(string wcfClassName)
        {
            string controllerName = $"{wcfClassName}Controller";

            return controllerName;
        }

        public static string GetDefaultControllerNamespace()
        {
            return "ApiServices";
        }

        public static string MapToControllersNamespace(string oldNamespace)
        {
            string newNamespace = $"{oldNamespace}.Services";

            return newNamespace;
        }

        public static string MapToRouteUriTemplate(string controllerName, string methodName)
        {
            string ConvertToHyphenDelimitedLowercase(string name)
            {
                // https://gist.github.com/vkobel/d7302c0076c64c95ef4b
                return String.Concat(name.Select((x, i) => i > 0 && Char.IsUpper(x) ? "-" + x.ToString() : x.ToString())) .ToLower();
            }

            var firstUriPart = controllerName.Replace("Controller", "").ToLower();
            var secondUriPart = ConvertToHyphenDelimitedLowercase(methodName);

            var routeUriTemplate = $"{firstUriPart}/{secondUriPart}";

            return routeUriTemplate;
        }

        public static string GetServiceTypeName(bool isAsmx)
        {
            string name = isAsmx
                ? "ASMX"
                : "WCF";

            return name;
        }
    }
}