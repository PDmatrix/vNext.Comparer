using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace vNext.Comparer.Utils
{
    public static class CompareHelper
    {
        public struct Differ
        {
            public string ObjectName { get; }
            public string LeftOriginalText { get; }
            public string RightOriginalText { get; }

            public Differ(string objectName, string leftOriginalText, string rightOriginalText)
            {
                ObjectName = objectName;
                LeftOriginalText = leftOriginalText;
                RightOriginalText = rightOriginalText;

            }
        }

        public static string AdjustForCompare(string content)
        {
            var reHeader = new Regex(@"^([\s\S])*(ALTER|CREATE)\s+PROCEDURE");
            return Regex.Replace(reHeader.Replace(content, string.Empty), @"\s+", string.Empty)
                .ToUpper();
        }

        public static void ProcWinMerge(IEnumerable<Differ> diff, string leftDir, string rightDir)
        {
            var enumerable = diff as Differ[] ?? diff.ToArray();
            const string winmergeuExe = "winmergeu.exe";
            if (enumerable.Length == 1)
            {
                var objectName = enumerable.First().ObjectName;
                var leftFilePath = Path.Combine(leftDir, objectName + ".sql");
                var rightFilePath = Path.Combine(rightDir, objectName + ".sql");

                var arguments = $@"""{leftFilePath}"" ""{rightFilePath}""";
                Process.Start(winmergeuExe, arguments);
            }
            else
            {
                Process.Start(winmergeuExe, $"/r {leftDir} {rightDir}");
            }
        }
    }
}