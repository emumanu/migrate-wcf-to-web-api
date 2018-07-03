using System;

namespace MigrateWcfToWebApi.Core.Mappers
{
    internal static class NamesMapper
    {
        public static string ConvertFirstCharToUppercase(string s)
        {
            // https://stackoverflow.com/a/3565041/4872
            return Char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}