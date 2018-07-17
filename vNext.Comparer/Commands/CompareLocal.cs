using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                throw new FileNotFoundException("No scripts in DIR path.");

            var notExists = (await GetNotExists(_connectionstring, dirFiles))
                .OrderBy(x => x)
                .ToArray();

            var exists = dirFiles
                .Except(notExists)
                .OrderBy(x => x)
                .ToArray();

            var diff = (await GetDiff(exists)).ToArray();

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
                var objName = Path.GetFileNameWithoutExtension(file);
                if (!await SqlHelper.IsObjectExists(connectionString, objName))
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

                var fileTextOriginal = await FileHelper.ReadText(file);
                var fileText = CompareHelper.AdjustForCompare(fileTextOriginal);
                var sqlTextOriginal = await SqlHelper.GetObjectDefinition(_connectionstring, objectName);
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
            if (Directory.Exists(DbDir))
            {
                Directory.Delete(DbDir, true);
            }
            Directory.CreateDirectory(DbDir);

            foreach (var differ in diff)
            {
                var path = Path.Combine(DbDir, differ.ObjectName + ".sql");
                File.WriteAllText(path, differ.LeftOriginalText, Encoding.UTF8);
                Console.WriteLine(@"Diff    {0}", differ.ObjectName);
            }
        }
    }
}