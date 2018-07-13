using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using vNext.Comparer.Commands;

namespace vNext.Comparer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var dict = ParseArgs(args);
            try
            {
                CommandFactory.Create(dict).Execute().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Console.WriteLine("Done.");
        }

        private static Dictionary<string, string> ParseArgs(IEnumerable<string> args)
        {
            return args
                .Distinct()
                .Select(arg => arg.Split('='))
                .ToDictionary(pair => pair[0].ToUpperInvariant(),
                    pair => pair.Length > 1 ? string.Join("=", pair.Skip(1)) : "");
        }
    }
}