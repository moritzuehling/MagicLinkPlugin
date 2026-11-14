using System;
using System.Threading.Tasks;

namespace MagicLinkTester
{
    // Prints the messages MagicLinkPlugin would send for each URL given, or "(nothing)".
    class Program
    {
        static async Task Main(string[] args)
        {
            foreach (var url in args)
            {
                Console.WriteLine("== " + url);
                var res = await MagicLinkPlugin.LinkHandler.GetContent(url);
                Console.WriteLine(res == null ? "(nothing)" : string.Join(Environment.NewLine + "--" + Environment.NewLine, res));
            }
        }
    }
}
