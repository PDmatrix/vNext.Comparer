using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    public class CompareLocal : ICommand
    {
        private const string DbDir = "db";
        private readonly string _dir;
        private readonly string _connectionstring;
        private readonly bool _winmerge;

        public CompareLocal(IDictionary<string, string> args)
        {
            ThrowExceptionForInvalidArgs(args);
            _dir = args["DIR"];
            _connectionstring = args["CONNECTIONSTRING"];
            _winmerge = args.ContainsKey("WINMERGE");
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
            var dirFiles = Directory.GetFiles(_dir, "*.sql");
            if (dirFiles.Length == 0)
            {
                throw new FileNotFoundException("No scripts in DIR path.");
            }

            var notExists = (await GetNotExists(_connectionstring, dirFiles).ConfigureAwait(false))
                .OrderBy(x => x)
                .ToArray();

            var exists = dirFiles
                .Except(notExists)
                .OrderBy(x => x)
                .ToArray();

            var diff = (await GetDiff(exists).ConfigureAwait(false)).ToArray();

            ProcNotExists(notExists);
            ProcDiff(diff);

            if(_winmerge && diff.Length >= 1)
            {
                CompareHelper.ProcWinMerge(diff, DbDir, _dir);
            }
        }

        private static async Task<string[]> GetNotExists(string connectionString, IEnumerable<string> dirFiles)
        {
            var list = new List<string>();
            foreach (var file in dirFiles)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);
                if (!await SqlHelper.IsObjectExistsAsync(connectionString, objectName).ConfigureAwait(false))
                {
                    list.Add(file);
                }
            }

            return list.ToArray();
        }

        private async Task<IEnumerable<CompareHelper.Differ>> GetDiff(IEnumerable<string> exists)
        {
            var list = new List<CompareHelper.Differ>();
            foreach (var file in exists)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);

                var fileTextOriginal = await FileHelper.ReadTextUtf8Async(file).ConfigureAwait(false);
                var fileText = CompareHelper.AdjustForCompare(fileTextOriginal);
                var sqlTextOriginal = await SqlHelper.GetObjectDefinitionAsync(_connectionstring, objectName).ConfigureAwait(false);
                var sqlText = CompareHelper.AdjustForCompare(sqlTextOriginal);

                if (sqlText != fileText)
                {
                    list.Add(new CompareHelper.Differ(objectName, sqlTextOriginal, fileTextOriginal));
                }
            }

            return list;
        }

        private static void ProcNotExists(IEnumerable<string> notExists)
        {
            foreach (var file in notExists)
            {
                Console.WriteLine(@"Not in DB     {0}", Path.GetFileNameWithoutExtension(file));
            }
        }

        private static void ProcDiff(IEnumerable<CompareHelper.Differ> diff)
        {
            FileHelper.CreateDirectory(DbDir);

            foreach (var differ in diff)
            {
                FileHelper.WriteInFile(Path.Combine(DbDir, differ.ObjectName + ".sql"), differ.LeftOriginalText);
                Console.WriteLine(@"Diff    {0}", differ.ObjectName);
            }
        }
    }
}