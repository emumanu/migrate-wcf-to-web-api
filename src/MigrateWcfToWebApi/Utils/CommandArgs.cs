using System;
using System.Collections.Generic;
using System.Linq;
using static System.Tuple;

namespace MigrateWcfToWebApi.Utils
{
    internal static class CommandArgs
    {
        internal class ArgValues
        {
            public bool IsHelp;
            public string WcfServiceDir;
            public string ServiceOutputDir;
            public string WcfClientDir;
            public string ClientOutputDir;
            public bool IsInvalid;
        }

        public static ArgValues Get(string[] args)
        {
            var cmdArgs = new ArgValues();

            if (args.Length <= 0)
            {
                cmdArgs.IsInvalid = true;

                return cmdArgs;
            }

            var hasValidArgs = new List<bool>();

            // get param values
            var paramList = new List<Tuple<string, Action<string>>>
            {
                Create<string, Action<string>>("wcfServiceDir", paramValue => cmdArgs.WcfServiceDir = paramValue),
                Create<string, Action<string>>("serviceOutputDir", paramValue => cmdArgs.ServiceOutputDir = paramValue),
                Create<string, Action<string>>("wcfClientDir", paramValue => cmdArgs.WcfClientDir = paramValue),
                Create<string, Action<string>>("clientOutputDir", paramValue => cmdArgs.ClientOutputDir = paramValue),
            };

            foreach (var paramItem in paramList)
            {
                var paramKey = $"-{paramItem.Item1}:";
                var fullArg = args.FirstOrDefault(arg => arg.ToLower().StartsWith(paramKey.ToLower()));
                var paramValue = fullArg?
                    .Replace(paramKey, "")
                    .Replace("\"", "");

                var setCmdArg = paramItem.Item2;
                setCmdArg(paramValue);

                hasValidArgs.Add(!string.IsNullOrEmpty(fullArg));
            }

            // help
            var helpParam = args.FirstOrDefault(arg => new[] {"-?", "/?"}.Any(arg.StartsWith));
            cmdArgs.IsHelp = !string.IsNullOrEmpty(helpParam);
            hasValidArgs.Add(cmdArgs.IsHelp);

            // invalid
            cmdArgs.IsInvalid = hasValidArgs.All(arg => arg == false);

            return cmdArgs;
        }
    }
}