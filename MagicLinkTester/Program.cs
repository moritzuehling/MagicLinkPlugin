using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicLinkTester
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                Console.Write("> ");
                var url = Console.ReadLine();

                var resolved = MagicLinkPlugin.ImageResolvers.Resolve(new Uri(url));
                resolved.Wait();
                Console.WriteLine("Resolved to: {0} (StopResolving: {1})", resolved.Result.Uri, resolved.Result.StopResolving);

                return; 
                /* 
                var image = MagicLinkPlugin.ImageHander.GetImage(url);
                image.Wait();

                if (image.Result != null)
                {
                    Console.WriteLine("Image: {0}...", image.Result.Substring(0, Console.BufferWidth - 15));
                }
                */
            }
        }
    }
}
