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

            if (_winmerge && diff.Length >= 1)
                CompareHelper.ProcWinMerge(diff, LeftDbDir, RightDbDir);
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

        private static async Task<IEnumerable<CompareHelper.Differ>> GetDiff(IEnumerable<string> exists)
        {
            var list = new List<CompareHelper.Differ>();
            foreach (var objectName in exists)
            {
                var leftSqlTextOriginal = await SqlHelper.GetObjectDefinition(_leftConnectionString, objectName);
                var rightSqlTextOriginal = await SqlHelper.GetObjectDefinition(_rightConnectionString, objectName);
                var leftSqlText = CompareHelper.AdjustForCompare(leftSqlTextOriginal);
                var rightSqlText = CompareHelper.AdjustForCompare(rightSqlTextOriginal);

                if (leftSqlText == rightSqlText) continue;

                list.Add(new CompareHelper.Differ(objectName, leftSqlTextOriginal, rightSqlTextOriginal));
            }
            return list;
        }

        private static void WriteInDirectory(CompareHelper.Differ differ)
        {
            var path = Path.Combine(LeftDbDir, differ.ObjectName + ".sql");
            File.WriteAllText(path, differ.LeftOriginalText);

            path = Path.Combine(RightDbDir, differ.ObjectName + ".sql");
            File.WriteAllText(path, differ.RightOriginalText);
        }

        private static void PrepareDirectory()
        {
            if (Directory.Exists(LeftDbDir))
                Directory.Delete(LeftDbDir, true);
            if (Directory.Exists(RightDbDir))
                Directory.Delete(RightDbDir, true);
            Directory.CreateDirectory(LeftDbDir);
            Directory.CreateDirectory(RightDbDir);
        }

        private static void ProcDiff(IEnumerable<CompareHelper.Differ> diff)
        {
            PrepareDirectory();
            foreach (var differ in diff)
            {
                Console.WriteLine(@"Diff    {0}", differ.ObjectName);
                WriteInDirectory(differ);
            }
        }
    }
}