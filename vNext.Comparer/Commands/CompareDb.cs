using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly string _leftConnectionString;
        private readonly string _rightConnectionString;
        private readonly string _query;
        private readonly bool _winmerge;


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
            {
                throw new ArgumentNullException(nameof(dict));
            }

            if (!dict.ContainsKey("LEFTCONNECTIONSTRING"))
            {
                throw new ArgumentException("Need to pass argument: LEFTCONNECTIONSTRING");
            }

            if (!dict.ContainsKey("RIGHTCONNECTIONSTRING"))
            {
                throw new ArgumentException("Need to pass argument: LEFTCONNECTIONSTRING");
            }
        }

        public async Task Execute()
        {
            await RunAsync().ConfigureAwait(false);
        }

        private async Task RunAsync()
        {
            var existsInLeft = (await GetExists(_leftConnectionString).ConfigureAwait(false))
                .OrderBy(x => x)
                .ToArray();

            var existsInRight = (await GetExists(_rightConnectionString).ConfigureAwait(false))
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
            {
                CompareHelper.ProcWinMerge(diff, LeftDbDir, RightDbDir);
            }
        }

        private async Task<string[]> GetExists(string connectionString)
        {
            return (await SqlHelper.GetDbObjects(connectionString, _query).ConfigureAwait(false)).ToArray();
        }

        private static void ProcNotExists(IEnumerable<string> notExists, string rightOrLeft)
        {
            foreach (var notExist in notExists)
            {
                Console.WriteLine($"Not in {rightOrLeft} DB    {notExist}");
            }
        }

        private async Task<IEnumerable<CompareHelper.Differ>> GetDiff(IEnumerable<string> exists)
        {
            var list = new List<CompareHelper.Differ>();
            foreach (var objectName in exists)
            {
                var leftSqlTextOriginal = await SqlHelper.GetObjectDefinition(_leftConnectionString, objectName).ConfigureAwait(false);
                var rightSqlTextOriginal = await SqlHelper.GetObjectDefinition(_rightConnectionString, objectName).ConfigureAwait(false);
                var leftSqlText = CompareHelper.AdjustForCompare(leftSqlTextOriginal);
                var rightSqlText = CompareHelper.AdjustForCompare(rightSqlTextOriginal);

                if (leftSqlText == rightSqlText)
                {
                    continue;
                }

                list.Add(new CompareHelper.Differ(objectName, leftSqlTextOriginal, rightSqlTextOriginal));
            }
            return list;
        }

        private static void WriteInDirectory(CompareHelper.Differ differ)
        {
            var path = Path.Combine(LeftDbDir, differ.ObjectName + ".sql");
            File.WriteAllText(path, differ.LeftOriginalText, Encoding.UTF8);

            path = Path.Combine(RightDbDir, differ.ObjectName + ".sql");
            File.WriteAllText(path, differ.RightOriginalText, Encoding.UTF8);
        }

        private static void PrepareDirectory()
        {
            if (Directory.Exists(LeftDbDir))
            {
                Directory.Delete(LeftDbDir, true);
            }

            if (Directory.Exists(RightDbDir))
            {
                Directory.Delete(RightDbDir, true);
            }

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