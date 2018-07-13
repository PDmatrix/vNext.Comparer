using System.Text.RegularExpressions;

namespace vNext.Comparer.Utils
{
    public static class CompareHelper
    {
        public static string AdjustForCompare(string content)
        {
            var reHeader = new Regex(@"^([\s\S])*(ALTER|CREATE)\s+PROCEDURE");
            return Regex.Replace(reHeader.Replace(content, string.Empty), @"\s+", string.Empty)
                .ToUpper();
        }
    }
}