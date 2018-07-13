using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using vNext.Comparer.Utils;

namespace vNext.Comparer.Commands
{
    internal class CompareDb : ICommand
    {
        private const string AllProcQuery =
            "select distinct SCHEMA_NAME(schema_id) + '.' + name from sys.procedures where type = 'P'";
        private const string LeftDbDir = "leftDb";
        private const string RightDbDir = "rightDb";
        private static string _leftConnectionString;
        private static string _rightConnectionString;
        private static string _query;
        private static bool _winmerge;


        public CompareDb(IDictionary<string, string> args)
        {
            ThrowExceptionForInvalidArgs(args);
            _leftConnectionString = args["LEFTCONNECTIONSTRING"];
            _rightConnectionString = args["RIGHTCONNECTIONSTRING"];
            _query = args.ContainsKey("QUERY") ? args["QUERY"] : AllProcQuery;
            _winmerge = args.ContainsKey("WINMERGE");
        }

        private static void ThrowExceptionForInvalidArgs(IDictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey("LEFTCONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: LEFTCONNECTIONSTRING");

            if (!dict.ContainsKey("RIGHTCONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: LEFTCONNECTIONSTRING");
        }

        public async Task Execute()
        {
            await RunAsync();
        }

        private static async Task RunAsync()
        {
            var existsInLeft = (await GetExists(_leftConnectionString))
                .OrderBy(x => x)
                .ToArray();

            var existsInRight = (await GetExists(_rightConnectionString))
                .OrderBy(x => x)
                .ToArray();

            var exitstInBoth = existsInRight.Intersect(existsInLeft)
                .OrderBy(x => x)
                .ToArray();

            var notExistsInLeft = existsInLeft.Except(exitstInBoth)
                .OrderBy(x => x)
                .ToArray();

            var notExistsInRight = existsInRight.Except(exitstInBoth)
                .OrderBy(x => x)
                .ToArray();

            ProcNotExists(notExistsInLeft, "Right");
            ProcNotExists(notExistsInRight, "Left");

            var diff = (await GetDiff(exitstInBoth)).ToArray();
            ProcDiff(diff);
            ProcWinMerge(diff);
        }

        private static async Task<string[]> GetExists(string connectionString)
        {
            return (await SqlHelper.GetDbObjects(connectionString, _query)).ToArray();
        }

        private static void ProcNotExists(IEnumerable<string> notExists, string rightOrLeft)
        {
            foreach (var notExist in notExists)
                Console.WriteLine($"Not in {rightOrLeft} DB    {notExist}");
        }

        private static async Task<IEnumerable<string>> GetDiff(IEnumerable<string> exists)
        {
            var list = new List<string>();
            DirectoryPreparation();
            foreach (var procedure in exists)
            {
                var leftSqlTextOriginal = await SqlHelper.GetObjectDefinition(_leftConnectionString, procedure);
                var rightSqlTextOriginal = await SqlHelper.GetObjectDefinition(_rightConnectionString, procedure);
                var leftSqlText = CompareHelper.AdjustForCompare(leftSqlTextOriginal);
                var rightSqlText = CompareHelper.AdjustForCompare(rightSqlTextOriginal);

                if (leftSqlText == rightSqlText) continue;

                list.Add(procedure);

                WriteInDirectory(leftSqlTextOriginal, LeftDbDir, procedure);
                WriteInDirectory(rightSqlTextOriginal, RightDbDir, procedure);
            }

            return list;
        }

        private static void WriteInDirectory(string textOrig, string directory, string filename)
        {
            var path = Path.Combine(directory, filename + ".sql");
            File.WriteAllText(path, textOrig);
        }

        private static void DirectoryPreparation()
        {
            if (Directory.Exists(LeftDbDir))
                Directory.Delete(LeftDbDir, true);
            if (Directory.Exists(RightDbDir))
                Directory.Delete(RightDbDir, true);
            Directory.CreateDirectory(LeftDbDir);
            Directory.CreateDirectory(RightDbDir);
        }

        private static void ProcDiff(IEnumerable<string> diff)
        {
            foreach (var procedure in diff) Console.WriteLine(@"Diff    {0}", procedure);
        }
        
        private static void ProcWinMerge(IEnumerable<string> diff)
        {
            var enumerable = diff as string[] ?? diff.ToArray();
            if (!_winmerge || !enumerable.Any())
                return;

            const string winmergeuExe = "winmergeu.exe";
            if (enumerable.Length == 1)
            {
                var objectName = enumerable.First();
                var leftFilePath = Path.Combine(LeftDbDir, objectName + ".sql");
                var rightFilePath = Path.Combine(RightDbDir, objectName + ".sql");

                var arguments = $@"""{leftFilePath}"" ""{rightFilePath}""";
                Process.Start(winmergeuExe, arguments);
            }
            else
            {
                Process.Start(winmergeuExe, $"/r {LeftDbDir} {RightDbDir}");
            }
        }
    }
}