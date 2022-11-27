using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using CommandLine;
using System.Text.RegularExpressions;
using System.Timers;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace entrypoint
{

    class CLI
    {
        private bool debug = false;
        private Thread crawler;
        private Thread scraper;
        private List<string> scopes;
        private string root_url;
        private string user_agent;
        private ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> queue2 = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, string> data = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<string, List<string>> result = new ConcurrentDictionary<string, List<string>>();
        private List<string> analyzed = new List<string>();
        private Stopwatch timer = new Stopwatch();
        private string regex_pattern;

        private class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
            public bool Verbose { get; set; }
            [Option('t', "target", Required = true, HelpText = "Set target host.")]
            public string Target{ get; set; }
            [Option('s', "scope", Required = true, HelpText = "Allowed domain scope, use ; as delimiter.")]
            public string Scope { get; set; }
            [Option('a', "agent", Required = false, HelpText = "Set custom user agent.", Default = "Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1")]
            public string Agent { get; set; }
            [Option('p', "pattern", Required = true, HelpText = "Regex pattern to scrape with.")]
            public string Pattern { get; set; }
        }

        public static void Main(string[] args)
        {// Entrypoint
            Console.WriteLine("");
            Console.WriteLine("  ▄      ▄   ██▄   ▄███▄   ▄████  ▄█    ▄   ▄███▄   ██▄              ▄▄▄▄▄   ▄█▄    █▄▄▄▄ ██   █ ▄▄  ▄███▄   █▄▄▄▄ ");
            Console.WriteLine("   █      █  █  █  █▀   ▀  █▀   ▀ ██     █  █▀   ▀  █  █            █     ▀▄ █▀ ▀▄  █  ▄▀ █ █  █   █ █▀   ▀  █  ▄▀ ");
            Console.WriteLine("█   █ ██   █ █   █ ██▄▄    █▀▀    ██ ██   █ ██▄▄    █   █         ▄  ▀▀▀▀▄   █   ▀  █▀▀▌  █▄▄█ █▀▀▀  ██▄▄    █▀▀▌  ");
            Console.WriteLine("█   █ █ █  █ █  █  █▄   ▄▀ █      ▐█ █ █  █ █▄   ▄▀ █  █           ▀▄▄▄▄▀    █▄  ▄▀ █  █  █  █ █     █▄   ▄▀ █  █  ");
            Console.WriteLine("█▄ ▄█ █  █ █ ███▀  ▀███▀    █      ▐ █  █ █ ▀███▀   ███▀                     ▀███▀    █      █  █    ▀███▀     █   ");
            Console.WriteLine(" ▀▀▀  █   ██                 ▀       █   ██                                          ▀      █    ▀            ▀    ");
            Console.WriteLine("                                                                                           ▀                       ");
            Console.WriteLine("");
            new CLI(args);
        }

        public CLI(string[] args) {             
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(
                o =>
                {
                    queue.Enqueue(o.Target);
                    debug = o.Verbose;
                    root_url = o.Target;
                    user_agent = o.Agent;
                    scopes = o.Scope.Split(';').ToList();
                    regex_pattern = o.Pattern;
                    crawler = new Thread(Crawler);
                    scraper = new Thread(Scraper);
                }
            );
            Console.WriteLine($"|  Verbose: {debug}  |  Target: {root_url}  | ");

            Console.WriteLine("|  Configured scope ->");
            foreach (string scope in scopes) {
                Console.WriteLine($"|    - {scope}");
            }
            Console.WriteLine();
            Console.WriteLine("Processing ...");
            timer.Start();
            Start();
            Console.WriteLine("Main method complete. Press any key to finish");
            Console.ReadKey();
        }

        private MatchCollection regex(string pattern, string data)
        {// Executes regex <pattern> on provided <data>
            Regex rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection matches = rx.Matches(data);
            return matches;
        }

        private string request(string url)
        {// Makes requests to <url> and read content of the source code which will be returned

            if (root_url.EndsWith("/") && url.StartsWith("/"))
            {
                url = root_url.Remove(root_url.Length - 1, 1) + url;
            }
            else if (url.StartsWith("/"))
            { 
                url = root_url + url;
            }
            HttpWebRequest theRequest = (HttpWebRequest)WebRequest.Create(url);
            theRequest.Headers["user-agent"] = user_agent;
            theRequest.Method = "GET";
            try
            {
                WebResponse theResponse = theRequest.GetResponse();
                StreamReader sr = new StreamReader(theResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                string result = sr.ReadToEnd();
                sr.Close();
                theResponse.Close();
                return result;
            }
            catch (WebException)
            {
                return "";
            }
        }

        public void Start()
        {
            Task _crawler = new Task(() => { crawler.Start(); });
            _crawler.Start();
            Task _scraper = new Task(() => { scraper.Start(); });
            _scraper.Start();
            Task.WaitAll(_crawler, _scraper);
        }

        private void Crawler()
        {
            string target;
            while (queue.TryDequeue(out target))
            {
                string response = request(target);
                MatchCollection matches = regex("href=\"(.*)\"", response);
                data.TryAdd(target, response);
                queue2.Enqueue(target);
                foreach (Match match in matches)
                {
                    string m = match.Groups[1].Value.Split("\"")[0];
                    foreach (string scope in scopes)
                    {
                        if (m.StartsWith(scope) && !analyzed.Contains(m))
                        {
                            if (debug) { 
                                Console.WriteLine($"|  ANALYZING: {m}");
                            }
                            analyzed.Add(m);
                            queue.Enqueue(m);
                        }
                        //else {
                        //    Console.WriteLine($"|  SKIPPING: {m}");
                        //}
                    }
                }
                Console.Clear();
                Console.WriteLine($"|  Analyzed: {analyzed.Count()}");
                Console.WriteLine($"|  Total in queue: {queue.Count()}");
                Console.WriteLine($"|  Ready for scraping: {queue2.Count()}");
                Console.WriteLine($"|  Total Scraped: {result.Count()}");
                Console.WriteLine($"|  Running time: {timer.Elapsed}");
            }
        }

        private void Scraper()
        {
            while (data.Count() == 0) { 
                Thread.Sleep(2000);
            }
            string target;
            while (queue2.Count() > 0 || queue.Count() > 0 || crawler.IsAlive) {
                while (queue2.TryDequeue(out target))
                {
                    string content;
                    data.TryRemove(target, out content);
                    if (debug) { 
                        Console.WriteLine($"|  SCRAPING: {target}");
                    }
                    MatchCollection matches = regex(regex_pattern, content);
                    List<string> converted_matches = new List<string>(matches.Cast<Match>().Select(m => m.Value).ToArray());
                    result.TryAdd(target, converted_matches);
                }
                //Thread.Sleep(30000);
                try
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\results.json", JsonConvert.SerializeObject(result));
                }
                catch { }
            }

            if (debug)
            {
                Console.WriteLine($"|  Analyzed: {analyzed.Count()}");
                Console.WriteLine($"|  Total in queue: {queue.Count()}");
                Console.WriteLine($"|  Ready for scraping: {queue2.Count()}");
                Console.WriteLine($"|  Total Scraped: {result.Count()}");
                Console.WriteLine($"|  Running time: {timer.Elapsed}");
            }
            Console.Write(JsonConvert.SerializeObject(result));
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"\results.json", JsonConvert.SerializeObject(result));
            Console.WriteLine($"|  Finished!");
        }
    }
}