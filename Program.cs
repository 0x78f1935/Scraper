using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommandLine;

namespace Scraper
{
    class Cli
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('t', "target", Required = true, HelpText = "Set target host.")]
            public string Target{ get; set; }
            [Option('s', "scope", Required = true, HelpText = "Allowed domain scope, use ; as delimiter.")]
            public string Scope { get; set; }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("  ▄      ▄   ██▄   ▄███▄   ▄████  ▄█    ▄   ▄███▄   ██▄              ▄▄▄▄▄   ▄█▄    █▄▄▄▄ ██   █ ▄▄  ▄███▄   █▄▄▄▄ ");
            Console.WriteLine("   █      █  █  █  █▀   ▀  █▀   ▀ ██     █  █▀   ▀  █  █            █     ▀▄ █▀ ▀▄  █  ▄▀ █ █  █   █ █▀   ▀  █  ▄▀ ");
            Console.WriteLine("█   █ ██   █ █   █ ██▄▄    █▀▀    ██ ██   █ ██▄▄    █   █         ▄  ▀▀▀▀▄   █   ▀  █▀▀▌  █▄▄█ █▀▀▀  ██▄▄    █▀▀▌  ");
            Console.WriteLine("█   █ █ █  █ █  █  █▄   ▄▀ █      ▐█ █ █  █ █▄   ▄▀ █  █           ▀▄▄▄▄▀    █▄  ▄▀ █  █  █  █ █     █▄   ▄▀ █  █  ");
            Console.WriteLine("█▄ ▄█ █  █ █ ███▀  ▀███▀    █      ▐ █  █ █ ▀███▀   ███▀                     ▀███▀    █      █  █    ▀███▀     █   ");
            Console.WriteLine(" ▀▀▀  █   ██                 ▀       █   ██                                          ▀      █    ▀            ▀    ");
            Console.WriteLine("                                                                                           ▀                       ");
            Console.WriteLine("");
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(
                o =>
                {
                    Console.WriteLine($"|  Verbose: {o.Verbose}  |  Target: {o.Target}  | ");
                    List<string> scopes = o.Scope.Split(';').ToList();
                    Console.WriteLine("|  Configured scope ->");
                    foreach (string scope in scopes) {
                        Console.WriteLine($"|    - {scope}");
                    }
                }
            );
        }
    }
}