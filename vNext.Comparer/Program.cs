using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using vNext.Comparer.Utils;

namespace vNext.Comparer
{
    internal class Program
    {
        private const string DbDir = "db";

        private static void Main(string[] args)
        {
            var dict = ParseArgs(args);
            ThrowExceptionForInvalidArgs(dict);
            TryRunAsync(dict).Wait();

            Console.WriteLine("Done.");
            //Console.ReadKey();
        }

        private static void ThrowExceptionForInvalidArgs(Dictionary<string, string> dict)
        {
            if (dict == null)
                throw new ArgumentNullException(nameof(dict));

            if (!dict.ContainsKey("CONNECTIONSTRING"))
                throw new ApplicationException("Need to pass argument: CONNECTIONSTRING");

            if (!dict.ContainsKey("DIR"))
                throw new ApplicationException("Need to pass argument: DIR");
        }

        private static async Task TryRunAsync(IDictionary<string, string> args)
        {
            try
            {
                await RunAsync(args);
            }
            catch (Exception x)
            {
                Console.WriteLine(x);
            }
        }

        private static async Task RunAsync(IDictionary<string, string> args)
        {            
            var dirFiles = Directory.GetFiles(args["DIR"], "*.sql");
            if (dirFiles.Length == 0)
                throw new ApplicationException("No scripts in DIR path.");

            var notExists = (await GetNotExists(args["CONNECTIONSTRING"], dirFiles))
                .OrderBy(x => x)
                .ToArray();

            var exists = dirFiles
                .Except(notExists)
                .OrderBy(x => x).ToArray();

            var diff = await GetDiff(args, exists);

            ProcNotExists(notExists);
            ProcDiff(diff);
            ProcWinMerge(args, diff);
        }

        private static void ProcNotExists(string[] notExists)
        {
            foreach (var file in notExists)
                Console.WriteLine(@"not     {0}", Path.GetFileNameWithoutExtension(file));
        }

        private static void ProcDiff(IDictionary<string, string> diff)
        {
            if (!Directory.Exists(DbDir))
                Directory.CreateDirectory(DbDir);

            foreach (var file in diff.Keys)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);
                var path = Path.Combine(DbDir, objectName + ".sql");
                File.WriteAllText(path, diff[file]);
                Console.WriteLine(@"diff    {0}", objectName);
            }            
        }

        private static void ProcWinMerge(IDictionary<string, string> args, IDictionary<string, string> diff)
        {
            if (!args.ContainsKey("WINMERGE") || diff.Count == 0)
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
                Process.Start(winmergeuExe, $"/r {DbDir} {args["DIR"]}");
            }
        }

        private static async Task<IDictionary<string, string>> GetDiff(IDictionary<string, string> args, string[] exists)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var file in exists)
            {
                var objectName = Path.GetFileNameWithoutExtension(file);
                var fileText = AdjustForCompare(await FileHelper.ReadText(file));
                var sqlTextOriginal = await SqlHelper.GetObjectDefinition(args["CONNECTIONSTRING"], objectName);
                var sqlText = AdjustForCompare(sqlTextOriginal);

                if (sqlText != fileText)
                    dictionary.Add(file, sqlTextOriginal);
            }
            return dictionary;
        }

        private static async Task<string[]> GetNotExists(string connectionString, string[] dirFiles)
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

        private static string AdjustForCompare(string content)
        {
            var reHeader = new Regex(@"^([\s\S])*(ALTER|CREATE)\s+PROCEDURE");
            return Regex.Replace(reHeader.Replace(content, string.Empty), @"\s+", string.Empty)
                    .ToUpper();
        }

        private static Dictionary<string, string> ParseArgs(string[] args)
        {
            return args
                .Distinct()
                .Select(arg => arg.Split('='))
                .ToDictionary(pair => pair[0].ToUpperInvariant(), pair => pair.Length > 1 ? string.Join("=", pair.Skip(1)) : "");
        }
    }
}
