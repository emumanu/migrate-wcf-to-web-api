using System.IO;
using System.Threading.Tasks;
using MigrateWcfToWebApi.Core.CodeGenerators.Client;
using MigrateWcfToWebApi.Core.CodeGenerators.Service;
using MigrateWcfToWebApi.Core.DataAccess;
using MigrateWcfToWebApi.Core.Models.Builders;
using MigrateWcfToWebApi.Utils;
using static System.Console;

namespace MigrateWcfToWebApi
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var cmdArgs = CommandArgs.Get(args);

            if (cmdArgs.IsHelp || cmdArgs.IsInvalid)
            {
                var usage = File.ReadAllText("./README.txt");
                WriteLine(usage);

                return;
            }

            WriteLine("starting...");

            WriteLine("preparing wcf service data...");
            var wcfServiceClasses = WcfServiceClassInfoBuilder.Create(cmdArgs.WcfServiceDir);
            await ConsoleMessaging.PrintMethodCounts(wcfServiceClasses);

            WriteLine("generating code files of service web api controllers from wcf data...");
            var serviceGenCodes = await ServiceCodeFilesGenerator.Run(wcfServiceClasses);

            WriteLine("writing code gen service controller files...");
            var serviceFilepaths = await CodeGenFilesAccess.WriteServiceFiles(cmdArgs.ServiceOutputDir, serviceGenCodes);
            ConsoleMessaging.PrintGeneratedFilepaths(serviceFilepaths);

            WriteLine("\n...service files done.\n");

            WriteLine("preparing wcf client data...");
            var wcfClientClasses = WcfClientClassInfoBuilder.Create(cmdArgs.WcfClientDir, serviceGenCodes, wcfServiceClasses);

            WriteLine("generating code files of client http requests from wcf data...");
            var clientGenCodes = await ClientCodeFilesGenerator.Run(wcfClientClasses);

            WriteLine("writing code gen client files...");
            var clientFilepaths = await CodeGenFilesAccess.WriteClientFiles(cmdArgs.ClientOutputDir, clientGenCodes);
            ConsoleMessaging.PrintGeneratedFilepaths(clientFilepaths);

            WriteLine("\n...client files done.\n");

            WriteLine("done!");
        }
    }
}
