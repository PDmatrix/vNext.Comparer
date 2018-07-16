using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    public class UpdateDb : ICommand
    {
        private static string _dir;
        private static string _connectionstring;
        private static bool _isFile;

        public UpdateDb(IDictionary<string, string> args)
        {
            ThrowExceptionForInvalidArgs(args);
            _connectionstring = args["CONNECTIONSTRING"];
            _dir = args["DIR"];
            _isFile = _dir.EndsWith(".sql");
        }

        private static void ThrowExceptionForInvalidArgs(IDictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey("CONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: CONNECTIONSTRING");

            if (!dict.ContainsKey("DIR"))
                throw new ApplicationException("Need to pass argument: DIR");
        }

        public async Task Execute()
        {
            await RunAsync();
        }

        private static async Task RunAsync()
        {
            var dirFiles = _isFile ? new []{ _dir } : Directory.GetFiles(_dir, "*.sql");

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