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
using System.IO;
using Newtonsoft.Json.Linq;

namespace entrypoint
{
    public static class ParallelExtention {
        public static IEnumerable<IEnumerable<T>> GetParrallelConsumingEnumerable<T>(this IProducerConsumerCollection<T> collection)
        {
            T item;
            while (collection.TryTake(out item))
            {
                yield return GetParrallelConsumingEnumerableInner(collection, item);
            }
        }

        private static IEnumerable<T> GetParrallelConsumingEnumerableInner<T>(IProducerConsumerCollection<T> collection, T item)
        {
            yield return item;
            while (collection.TryTake(out item))
            {
                yield return item;
            }
        }
    }

    class CLI  // CS1106
    {
        private bool debug = false;
        private string root_url;
        private List<string> scopes;
        private string user_agent;
        private string regex_pattern;
        private int maxConcurrentCrawlers;
        private int maxConcurrentScrapers;
        private int maxConcurrentDownloaders;
        private bool stripQueryParms;
        private bool DownloadImages;
        private bool GenerateOutput;
        private string Filename;
        private bool Checkpoints;
        private int total_downloads = 0;

        private Thread ThreadCrawler;
        private Thread ThreadScraper;
        private Thread ThreadDownloader;

        private ConcurrentQueue<string> QueueCrawler = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> QueueScraper = new ConcurrentQueue<string>();
        private ConcurrentQueue<string> QueueDownloads = new ConcurrentQueue<string>();
        private ConcurrentDictionary<string, string> MemoryQueue = new ConcurrentDictionary<string, string>();
        private ConcurrentDictionary<string, List<string>> result = new ConcurrentDictionary<string, List<string>>();
        private List<string> analyzed = new List<string>();
        private Stopwatch timer = new Stopwatch();

        private class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.", Default = false)]
            public bool Verbose { get; set; }
            [Option('t', "target", Required = true, HelpText = "Set target host.")]
            public string Target { get; set; }
            [Option('s', "scope", Required = true, HelpText = "Allowed domain scope, use ; as delimiter.")]
            public string Scope { get; set; }
            [Option('a', "agent", Required = false, HelpText = "Set custom user agent.", Default = "Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1")]
            public string Agent { get; set; }
            [Option('p', "pattern", Required = true, HelpText = "Regex pattern to scrape with.")]
            public string Pattern { get; set; }
            [Option('c', "crawlers", Required = false, HelpText = "Total concurrent tasks used for the Crawler.", Default = 4)]
            public int CCrawlers { get; set; }
            [Option('x', "scrapers", Required = false, HelpText = "Total concurrent tasks used for the Scraper.", Default = 4)]
            public int Cscrapers { get; set; }
            [Option('d', "downloaders", Required = false, HelpText = "Total concurrent downloaders used for downloading data.", Default = 2)]
            public int Cdownloaders { get; set; }
            [Option('q', "queryparameters", Required = false, HelpText = "Strip query parameters from URL(s).", Default = false)]
            public bool StripQueryParams { get; set; }
            [Option('i', "images", Required = false, HelpText = "Download found images.", Default = false)]
            public bool DownloadImages { get; set; }
            [Option('j', "json", Required = false, HelpText = "Generates output based on the pattern provided.", Default = false)]
            public bool GenerateOutput { get; set; }
            [Option('f', "filename", Required = false, HelpText = "The file name of the generated output.", Default = "result.json")]
            public string Filename { get; set; }
            [Option('k', "checkpoints", Required = false, HelpText = "Saves in between scraping pages, turn off to save time, might fail.", Default = false)]
            public bool Checkpoints { get; set; }
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

        public CLI(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(
                o =>
                {
                    QueueCrawler.Enqueue(o.Target);
                    debug = o.Verbose;
                    root_url = o.Target;
                    user_agent = o.Agent;
                    scopes = o.Scope.Split(';').ToList();
                    regex_pattern = o.Pattern;
                    maxConcurrentCrawlers = o.CCrawlers;
                    maxConcurrentScrapers = o.Cscrapers;
                    maxConcurrentDownloaders = o.Cdownloaders;
                    stripQueryParms = o.StripQueryParams;
                    DownloadImages = o.DownloadImages;
                    GenerateOutput = o.GenerateOutput;
                    Checkpoints = o.Checkpoints;
                    Filename = o.Filename;
                }
            );
            ThreadCrawler = new Thread(Crawler);
            ThreadScraper = new Thread(Scraper);
            ThreadDownloader = new Thread(Downloader);
            Console.WriteLine($"|  Verbose: {debug}  |  Target: {root_url}  | ");

            Console.WriteLine("|  Configured scope ->");
            foreach (string scope in scopes)
            {
                Console.WriteLine($"|    - {scope}");
            }
            Console.WriteLine();
            Console.WriteLine("Processing ...");
            timer.Start();
            Start();

            Console.WriteLine($"Main method completed, took: {timer.Elapsed}");
            Console.WriteLine("Press any key to close the program...");
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

        private string check_url(string uri) {
            string url = uri;
            if (!url.StartsWith(root_url))
            {
                if (root_url.EndsWith("/") && url.StartsWith("/"))
                {
                    url = root_url.Remove(root_url.Length - 1, 1) + url;
                }
                else if (url.StartsWith("/"))
                {
                    url = root_url + url;
                }
            }
            url = url.Replace("&amp;", "&").Replace("&amp", "&");
            return url;
        }

        public void Start()
        {
            Task _crawler = new Task(() => { ThreadCrawler.Start(); });
            _crawler.Start();
            Task _scraper = new Task(() => { ThreadScraper.Start(); });
            _scraper.Start();
            if (DownloadImages)
            {
                System.IO.Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Downloads");
                Task _downloader = new Task(() => { ThreadDownloader.Start(); });
                _downloader.Start();
                Task.WaitAll(_crawler, _scraper, _downloader);
            }
            else {
                Task.WaitAll(_crawler, _scraper);
            }
        }

        private void Crawler() {
            Parallel.ForEach(ParallelExtention.GetParrallelConsumingEnumerable(QueueCrawler), new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentCrawlers }, Items =>
            {
                foreach (string uri in Items)
                {
                    // Obtain raw HTML and temporarly store the data
                    string response = "";
                    if (scopes.Any(s => uri.StartsWith(s) | uri.StartsWith(root_url)))
                    {
                        string url = check_url(uri);
                        if (stripQueryParms && url.Contains("?")) {
                            url = url.Split("?")[0];
                        }
                        response = request(url);
                        MemoryQueue.TryAdd(url, response);
                        QueueScraper.Enqueue(url);
                    }

                    // Look for all available hrefs inside the HTML
                    MatchCollection matches = regex(@"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>[^>\s]+))", response);
                    foreach (Match match in matches)
                    {
                        string m = match.Groups[1].Value.Split("\"")[0];
                        if (!analyzed.Contains(m)) {
                            if (debug)
                            {
                                Console.WriteLine($"|  ANALYZING: {m}");
                            }

                            analyzed.Add(m);
                            if (scopes.Any(s => m.StartsWith(s))) {
                                QueueCrawler.Enqueue(m);
                            }
                        }
                    }
                    Console.Clear();
                    Console.WriteLine($"|  Analyzed: {analyzed.Count()}");
                    Console.WriteLine($"|  Total in queue: {QueueCrawler.Count()}");
                    Console.WriteLine($"|  Ready for scraping: {QueueScraper.Count()}");
                    Console.WriteLine($"|  Total Scraped: {result.Count()}");
                    Console.WriteLine($"|  Ready for download: {QueueDownloads.Count()}");
                    Console.WriteLine($"|  Total Downloads: {total_downloads}");
                    Console.WriteLine($"|  Running time: {timer.Elapsed}");
                }
                Console.WriteLine($"|  Crawler Finished: {timer.Elapsed}");
            });
        }


        private void Scraper()
        {
            while (QueueScraper.Count() > 0 || QueueCrawler.Count() > 0 || ThreadCrawler.IsAlive)
            {
                Parallel.ForEach(
                    ParallelExtention.GetParrallelConsumingEnumerable(QueueScraper),
                    new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentScrapers },
                    Items =>
                {
                    foreach (var target in Items)
                    {
                        string content;
                        MemoryQueue.TryRemove(target, out content);
                        if (debug)
                        {
                            Console.WriteLine($"|  SCRAPING: {target}");
                        }
                        if (content != null) {
                            MatchCollection matches = regex(regex_pattern, content);
                            List<string> converted_matches = new List<string>();
                            foreach (string uri in matches.Cast<Match>().Select(m => m.Value).ToArray()) { converted_matches.Add(uri); }
                            result.TryAdd(target, converted_matches);
                            MatchCollection images = regex(@"<img\b[^\<\>]+?\bsrc\s*=\s*[""'](?<L>.+?)[""'][^\<\>]*?\>", content);
                            List<string> converted_images = new List<string>();
                            foreach (Match uri in images) {
                                string url = check_url(uri.Groups[1].Value.Split("\"")[0]);
                                QueueDownloads.Enqueue(url);
                            }
                        }
                    }
                });
                if (Checkpoints && GenerateOutput)
                {
                    File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @$"\{Filename}", JsonConvert.SerializeObject(result));
                }
            }
            if (GenerateOutput) {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @$"\{Filename}", JsonConvert.SerializeObject(result));
            }
            Console.WriteLine($"|  Finished!");
        }

        private void Downloader()
        {
            while (QueueScraper.Count() > 0 || QueueCrawler.Count() > 0 || ThreadCrawler.IsAlive || ThreadScraper.IsAlive)
            {
                Parallel.ForEach(
                    ParallelExtention.GetParrallelConsumingEnumerable(QueueDownloads),
                    new ParallelOptions { MaxDegreeOfParallelism = maxConcurrentDownloaders },
                    Items =>
                {
                    foreach (string target in Items)
                    {
                        if (debug) { 
                            Console.WriteLine($"|  DOWNLOADING: {target}");
                        }
                        using (WebClient client = new WebClient())
                        {
                            client.Headers["user-agent"] = user_agent;
                            MatchCollection filename_match = regex(@"((.+\\)*(.+)\..{1,3})", target);
                            string filename = filename_match[0].Value.Split("/")[filename_match[0].Value.Split("/").Count() - 1];
                            string target_output_folder = AppDomain.CurrentDomain.BaseDirectory + @$"\Downloads\{filename}";
                            if (!File.Exists(target_output_folder)) { 
                                client.DownloadFile(new Uri(target), target_output_folder);
                                total_downloads += 1;
                            }
                        }
                    }
                });
            }
        }
    }
}