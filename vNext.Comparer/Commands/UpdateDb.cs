using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    internal class UpdateDb : ICommand
    {
        private static string _dir;
        private static string _file;
        private static string _connectionstring;

        public UpdateDb(IDictionary<string, string> args)
        {
            ThrowExceptionForInvalidArgs(args);
            _connectionstring = args["CONNECTIONSTRING"];

            if (args.ContainsKey("DIR"))
                _dir = args["DIR"];
            else
                _file = args["FILE"].EndsWith(".sql") ? args["FILE"] : args["FILE"] + ".sql";
        }

        private static void ThrowExceptionForInvalidArgs(IDictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey("CONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: CONNECTIONSTRING");

            if (!dict.ContainsKey("DIR") && !dict.ContainsKey("FILE"))
                throw new ApplicationException("Need to pass either DIR or FILE argument");

            if (dict.ContainsKey("DIR") && dict.ContainsKey("FILE"))
                throw new ApplicationException("Need to pass only DIR or FILE argument");
        }

        public async Task Execute()
        {
            if (_dir != null)
                await RunAsyncDir();
            else
                await RunAsyncFile();
        }

        private static async Task RunAsyncDir()
        {
            var dirFiles = Directory.GetFiles(_dir, "*.sql");
            if (dirFiles.Length == 0)
                throw new ApplicationException("No scripts in DIR path.");

            var notExists = (await GetNotExists(_connectionstring, dirFiles))
                .OrderBy(x => x)
                .ToArray();

            var exists = dirFiles
                .Except(notExists)
                .OrderBy(x => x)
                .ToArray();

            await ProcExists(exists);
            ProcNotExists(notExists);
        }

        private static async Task RunAsyncFile()
        {
            var objectName = Path.GetFileNameWithoutExtension(_file);
            if (await SqlHelper.IsObjectExists(_connectionstring, objectName))
                await SqlHelper.ExecuteNonQueryScriptAsync(_connectionstring, await FileHelper.ReadText(_file));
            else
                Console.WriteLine($"Not in DB   {objectName}");
        }

        private static async Task<string[]> GetNotExists(string connectionString, IEnumerable<string> dirFiles)
        {
            var list = new List<string>();
            foreach (var file in dirFiles)
            {
                var objName = Path.GetFileNameWithoutExtension(file);
                if (!await SqlHelper.IsObjectExists(connectionString, objName))
                    list.Add(file);
            }

            return list.ToArray();
        }

        private static async Task ProcExists(IEnumerable<string> exists)
        {
            foreach (var file in exists)
                await SqlHelper.ExecuteNonQueryScriptAsync(_connectionstring, await FileHelper.ReadText(file));
        }

        private static void ProcNotExists(IEnumerable<string> notExists)
        {
            foreach (var file in notExists)
            {
                var objName = Path.GetFileNameWithoutExtension(file);
                Console.WriteLine($"Not in DB    {objName}");
            }
        }
    }
}