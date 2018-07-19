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
        /// <summary>
        /// Returns the string without script header and white space characters in upper form.
        /// </summary>
        /// <param name="script">Script to adjust</param>
        /// <returns></returns>
        public static string AdjustForCompare(string script)
        {
            var reHeader = new Regex(@"^([\s\S])*(ALTER|CREATE)\s+(PROCEDURE|TABLE|VIEW)", RegexOptions.IgnoreCase);
            return Regex.Replace(reHeader.Replace(script, string.Empty), @"\s+", string.Empty)
                .ToUpper();
        }
        /// <summary>
        /// Opens the WinMerge to check differences in files or directories
        /// </summary>
        /// <param name="diff">The array of Differ struct</param>
        /// <param name="leftDir">Path to the left directory</param>
        /// <param name="rightDir">Path to the right directory</param>
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