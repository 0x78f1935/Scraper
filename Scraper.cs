using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using System.Diagnostics;

namespace Scraper
{
    internal class Scraper
    {
        private ConcurrentQueue<string> target_queue = new ConcurrentQueue<string>();
        private List<string> scopes;  // The scope which urls are allowed to start with
        private List<string> parsed = new List<string>();  // Keeps track of urls which already have been parsed
        private string target_base;  // Holds namespace base
        private string user_agent;    // A random one time generated user agent (with fallback)
        private Task crawler;  // Holds the crawler task
        private Stopwatch timer = new Stopwatch();

        public Scraper(string entry_target, List<string> scope) {
            timer.Start();
            user_agent = _user_agent();
            scopes = scope;
            target_base = entry_target;
            target_queue.Enqueue(entry_target);
            Console.WriteLine($"|  Obtained: {entry_target}");
            crawler = new Task(() => { _crawler(); });
            crawler.Start();
        }

        private MatchCollection regex(string pattern, string data)
        {// Executes regex <pattern> on provided <data>
            Regex rx = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection matches = rx.Matches(data);
            return matches;
        }

        private string request(string url)
        {// Makes requests to <url> and read content of the source code which will be returned
            if (url.StartsWith("/")) { 
                url = target_base + url;
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
            catch (WebException) {
                return "";
            }
        }

        private string _user_agent() 
        {// Makes request to service which generate random user agents, use fallback if no user agents are available
            string result = request("https://user-agents.net/random?limit=1&action=generate");
            MatchCollection matches = regex("Mozilla/5.0.+</a>", result);
            var random = new Random();
            int random_index = random.Next(matches.Count);
            string agent = matches[random_index].ToString().Replace("</a>", "");
            if (agent.Count() > 0) {
                Console.WriteLine($"|  [GOOD] Random User Agent");
                return agent;
            }
            // Fallback useragent
            Console.WriteLine($"|  [FALLBACK] Random User Agent");
            return "Mozilla/5.0 (Windows; U; Windows NT 6.2) AppleWebKit/534.2.1 (KHTML, like Gecko) Chrome/35.0.822.0 Safari/534.2.1";
        }

        private void _crawler() {
            Console.WriteLine($"|  Total in queue: {target_queue.Count()}");
            foreach(string t in target_queue)
            {
                string target;
                target_queue.TryDequeue(out target);
                ThreadPool.QueueUserWorkItem((o) => { 
                    string result = request(target);
                    MatchCollection matches = regex("href=\"(.*)\"", result);
                    if (matches.Count() > 0) {
                        Task filter = new Task(() => endpoint_filter(matches));
                        filter.Start();
                        filter.Wait();
                    }

                    Console.WriteLine($"|  Total in queue: {target_queue.Count()}");
                });
            }
        }

        private void endpoint_filter(MatchCollection matches) {
            foreach (Match match in matches)
            {
                string m = match.Groups[1].Value.Split("\"")[0];
                foreach (string scope in scopes) {
                    if (m.StartsWith(scope) && !parsed.Contains(m))
                    {
                        parsed.Add(m);
                        target_queue.Enqueue(m);
                        Console.WriteLine($"|  PARSING: {m}");
                    }
                    //else {
                    //    Console.WriteLine($"|  SKIPPING: {m}");
                    //}
                }
            }
            Console.WriteLine($"|  Parsed: {matches.Count()}");
            Console.WriteLine($"|  Total in queue: {target_queue.Count()}");
            Console.WriteLine($"|  Running time: {timer.Elapsed}");
            //if (target_queue.Count() > 0 && crawler.IsCompleted) {
            //    crawler = new Task(() => { _crawler(); });
            //    crawler.Start();
            //}
        }

        public async Task RunTasks()
        {
            var tasks = new List<Task>
        {
            new Task(() => { _crawler(); })
        };

        await Task.WhenAll(tasks);

            //Run the other tasks            
        }
    }
}
