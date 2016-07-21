using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args) {
            string seenPostIds = "seen_post_ids.txt";

            WebClient client = new WebClient();

            // Add a user agent header in case the 
            // requested URI contains a query.

            client.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0");
            client.Encoding = Encoding.UTF8;

            Queue<string> divs = new Queue<string>();
            Dictionary<string, bool> oldPosts = new Dictionary<string, bool>();
            if (File.Exists(seenPostIds)) {
                foreach (string postId in File.ReadAllLines(seenPostIds)) {
                    oldPosts[postId] = true;
                }
                File.Copy(seenPostIds, seenPostIds + ".bak", true);
            }
            int totalPosts = 0;
            int newPosts = 0;
            int index = 0;
            int i = 0;
            int page = 1;
            int numPagesToCheck = 3;
            string master = null;
            do {
                string suffix = "";
                if (page > 1) suffix = "?page=" + page;
                string html = client.DownloadString("https://forums.wildstar-online.com/forums/index.php?/devtracker/" + suffix);
                File.WriteAllText("input.html", html, Encoding.UTF8);
                while (true) {
                    Match match = Regex.Match(html, "<div[ >]|</div>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                    if (match.Success == false) break;
                    string tmpDivName = "tmpdiv";
                    switch (match.Length) {
                        case 5: //"<div " or "<div>"
                            tmpDivName = String.Format("<{0}{1}", tmpDivName, i++);
                            break;
                        case 6: //"</div>"
                            tmpDivName = String.Format("</{0}{1}", tmpDivName, --i);
                            break;
                    }
                    html = html.Remove(match.Index, match.Length - 1).Insert(match.Index, tmpDivName);
                    if (i < 0) {
                        Console.WriteLine("div index fell below 0");
                        Console.ReadLine();
                        return;
                    }
                }
                if (i != 0) {
                    Console.WriteLine("div index didn't end at 0");
                    Console.ReadLine();
                    return;
                }
                while (true) {
                    Match match = Regex.Match(html, @"<(tmpdiv\d+) id=""post_id_(\d+)""", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                    if (match.Success == false) break;
                    if (index == 0) index = match.Index;
                    string tmpDiv = match.Groups[1].ToString();
                    string postId = match.Groups[2].ToString();
                    string search = String.Format(@"<{0} (id=""post_id_{1}"".*?)</{0}>", tmpDiv, postId);
                    totalPosts++;
                    string div = Regex.Match(html, search, RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline).Value;
                    html = Regex.Replace(html, search, "", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline);
                    if (oldPosts.ContainsKey(postId) == false) {
                        divs.Enqueue(div);
                        oldPosts[postId] = true;
                        newPosts++;
                    }
                }
                if (master == null) master = html;
                page++;
                if (page > numPagesToCheck) {
                    Console.WriteLine("Processed " + totalPosts + " total posts.");
                    Console.WriteLine("Found " + newPosts + " new posts.");
                    Console.Write("Check next page? (y/n): ");
                }
            } while (page <= numPagesToCheck || Console.ReadLine() == "y");
            foreach (string div in divs) {
                master = master.Insert(index, div);
            }
            master = Regex.Replace(master, @"tmpdiv\d+", "div");
            File.WriteAllText("output.html", master, Encoding.UTF8);
            File.WriteAllLines(seenPostIds, oldPosts.Keys.ToArray());
            if (newPosts > 0) System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", "output.html");
        }
    }
}

//req
//https://forums.wildstar-online.com/forums/index.php?/devtracker/
//Host: forums.wildstar-online.com
//User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64; rv:44.0) Gecko/20100101 Firefox/44.0
//Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
//Accept-Language: en-US,en;q=0.5
//Accept-Encoding: gzip, deflate, br
//Cookie: _ga=GA1.2.303358399.1437684259; __utma=43664506.303358399.1437684259.1455571721.1455657840.34; __utmz=43664506.1455657840.34.32.utmcsr=google|utmccn=(organic)|utmcmd=organic|utmctr=(not%20provided); lang=en-US; rteStatus=rte; __qca=P0-2033685177-1447892335523
//Connection: keep-alive
//Cache-Control: max-age=0

//resp
//Cache-Control: no-cache, must-revalidate, max-age=0
//Connection: Keep-Alive
//Content-Encoding: gzip
//Content-Type: text/html;charset=UTF-8
//Date: Thu, 18 Feb 2016 15:24:30 GMT
//Expires: Wed, 17 Feb 2016 15:24:30 GMT
//Keep-Alive: timeout=2, max=50
//Pragma: no-cache
//Server: Apache/2.2.15 (CentOS)
//Set-Cookie: session_id=7463ac9c3413bee418aa28b816c7c9ab; path=/; httponly
//member_id=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Max-Age=0; path=/; httponly
//session_id=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Max-Age=0; path=/; httponly
//pass_hash=deleted; expires=Thu, 01-Jan-1970 00:00:01 GMT; Max-Age=0; path=/; httponly
//Transfer-Encoding: chunked
//Vary: Accept-Encoding