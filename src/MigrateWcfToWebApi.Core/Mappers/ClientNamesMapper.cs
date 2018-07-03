namespace MigrateWcfToWebApi.Core.Mappers
{
    internal static class ClientNamesMapper
    {
        public static string GetDefaultClientNamespace()
        {
            return "ApiServiceClients";
        }

        public static string MapToClientClassName(string className)
        {
            string clientClassName = $"{className}Client";

            return clientClassName;
        }

        public static string MapToInterfaceName(string className)
        {
            return $"I{className}";
        }

        public static string MapToClientNamespace(string oldNamespace)
        {
            var newNamespace = $"{oldNamespace}.Clients";

            return newNamespace;
        }

        public static string MapToClassNameIfInterfaceName(string className)
        {
            className = className.StartsWith("I")
                ? className.Substring(1)
                : className;

            return className;
        }
    }
}