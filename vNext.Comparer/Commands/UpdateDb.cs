using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    public class UpdateDb : ICommand
    {
        private readonly string _dir;
        private readonly string _connectionstring;
        private readonly bool _isFile;

        public UpdateDb(IDictionary<string, string> args)
        {
            ThrowExceptionForInvalidArgs(args);
            _connectionstring = args["CONNECTIONSTRING"];
            _dir = args["DIR"];
            _isFile = !File.GetAttributes(_dir).HasFlag(FileAttributes.Directory);
        }

        private static void ThrowExceptionForInvalidArgs(IDictionary<string, string> dict)
        {
            if (dict == null)
            {
                throw new ArgumentNullException(nameof(dict));
            }

            if (!dict.ContainsKey("CONNECTIONSTRING"))
            {
                throw new ArgumentException("Need to pass argument: CONNECTIONSTRING");
            }

            if (!dict.ContainsKey("DIR"))
            {
                throw new ArgumentException("Need to pass argument: DIR");
            }
        }

        public async Task Execute()
        {
            await RunAsync().ConfigureAwait(false);
        }

        private async Task RunAsync()
        {
            var dirFiles = _isFile ? new []{ _dir } : Directory.GetFiles(_dir, "*.sql");

            if (dirFiles.Length == 0)
            {
                throw new FileNotFoundException("No scripts in DIR path.");
            }

            await ProcExists(dirFiles).ConfigureAwait(false);
        }

        private async Task ProcExists(IEnumerable<string> exists)
        {
            foreach (var file in exists)
            {
                await SqlHelper.ExecuteNonQueryScriptAsync(_connectionstring, await FileHelper.ReadTextUtf8Async(file).ConfigureAwait(false));
            }
        }
    }
}