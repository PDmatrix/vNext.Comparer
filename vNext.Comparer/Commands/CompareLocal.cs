using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vNext.Comparer.Interface;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    internal class CompareLocal : ICommand
    {
        private const string DbDir = "db";
        private static string _dir;
        private static string _connectionstring;
        private static bool _winmerge;

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
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey("CONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: CONNECTIONSTRING");

            if (!dict.ContainsKey("DIR"))
                throw new ApplicationException("Need to pass argument: DIR");
        }

        public void Execute()
        {
            RunAsync().Wait();
        }

        private static async Task RunAsync()
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

            var diff = await GetDiff(exists);

            ProcNotExists(notExists);
            ProcDiff(diff);
            ProcWinMerge(diff);
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

        private static async Task<IDictionary<string, string>> GetDiff(IEnumerable<string> exists)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var file in exists)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);
                var fileText = CompareHelper.AdjustForCompare(await FileHelper.ReadText(file));
                var sqlTextOriginal = await SqlHelper.GetObjectDefinition(_connectionstring, objectName);
                var sqlText = CompareHelper.AdjustForCompare(sqlTextOriginal);

                if (sqlText != fileText)
                    dictionary.Add(file, sqlTextOriginal);
            }

            return dictionary;
        }

        private static void ProcNotExists(IEnumerable<string> notExists)
        {
            foreach (var file in notExists)
                Console.WriteLine(@"Not in DB     {0}", Path.GetFileNameWithoutExtension(file));
        }

        private static void ProcDiff(IDictionary<string, string> diff)
        {
            if (Directory.Exists(DbDir))
                Directory.Delete(DbDir, true);
            Directory.CreateDirectory(DbDir);

            foreach (var file in diff.Keys)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);
                var path = Path.Combine(DbDir, objectName + ".sql");
                File.WriteAllText(path, diff[file]);
                Console.WriteLine(@"Diff    {0}", objectName);
            }
        }

        private static void ProcWinMerge(IDictionary<string, string> diff)
        {
            if (!_winmerge || diff.Count == 0)
                return;

            const string winmergeuExe = "winmergeu.exe";
            if (diff.Count == 1)
            {
                var objectName = Path.GetFileNameWithoutExtension(diff.Keys.First());
                var leftFilePath = Path.Combine(DbDir, objectName + ".sql");
                var rightFilePath = diff.Keys.First();

                var arguments = $@"""{leftFilePath}"" ""{rightFilePath}""";
                Process.Start(winmergeuExe, arguments);
            }
            else
            {
                Process.Start(winmergeuExe, $"/r {DbDir} {_dir}");
            }
        }
    }
}