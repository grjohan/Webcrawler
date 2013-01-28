using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.IO;

namespace Webcrawler
{
    class Program
    {
        /// <summary>
        /// Main to start the webCrawler
        /// </summary>
        static void Main(string[] args)
        {
            new webCrawler("http://telenor.com");
            Console.WriteLine("Crawling in progress, files saved to current directory,");
            Console.WriteLine("to stop, please exit the program");
            while (true)
            {
            }
        }
    }
    /// <summary>
    /// webCrawler class that models a very simple webcrawler.
    /// Downloads websited and saves the to the current directory
    /// Then visit all links in that website that has not already been visited
    /// </summary>
    class webCrawler
    {
        HashSet<String> crawledUrls;
        BlockingCollection<String> urlsToCrawl = new BlockingCollection<String>();
        HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Constructor for the webCrawler class
        /// Creates a new webcrawler and starts 10 tasks to preform the crawling
        /// </summary>
        /// <param name="startUrl">String containing a Url on which to start crawling</param>
        public webCrawler(String startUrl)
        {

            crawledUrls = new HashSet<String>();
            crawledUrls.Add(startUrl);
            urlsToCrawl.Add(startUrl);
            // Create 10 tasks
            for (int i = 0; i < 10; i++)
            {
                // run all tasks async so tasks can be awaited
                Task.Run
                (
                 async () =>
                 {
                     while (true) // A task should repeat
                     {
                         string Url = urlsToCrawl.Take(); // try to get a new URL to crawl, will block if there is no URL
                         // The above line will deadlock the program if all tasks block on this because there are 0 Urls.
                         // but that will never happend realisticly since the internet will never run out of links
                         string page = await CrawlUrl(Url);
                         findUrls(page, Url);
                     }
                 }
                );
            }
        }

        /// <summary>
        /// Task to download a Url and save the data in the current directory
        /// </summary>
        /// <param name="Url">String containg the Url to download</param>
        /// <returns>The task represention the asych operation</returns>
        private async Task<string> CrawlUrl(String Url)
        {
            HttpResponseMessage response = await httpClient.GetAsync(Url); // await the page being downloaded 
            String page = await response.Content.ReadAsStringAsync();   // await reading the content of the HTTP response
            String path = Url.Substring(7).Replace('/', ' ') + ".html"; // remove the http:// and remove all / so the file can be saved
            File.WriteAllText(path, page); // save the website as a .html file in the current directory

            return (page);
        }

        /// <summary>
        /// Find all the HTML links in a string
        /// </summary>
        /// <param name="page">String to search for HTML links </param>
        /// <param name="curentUrl">String containg the URL theese links came from</param>
        private void findUrls(String page, String curentUrl)
        {
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(page); 
            HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//a"); // select all 'a' nodes/tags
            foreach (HtmlNode link in collection)
            {
                string url = link.Attributes["href"].Value; // get the href attributes value
                if (url.StartsWith("/"))    // if the url starts with / it is a relative url, so append it to current url
                    url = curentUrl + url;
                else if (url.StartsWith("?") || url.StartsWith("#")) // anchor or get data
                    break; // anchors or get data link to the same page, so theese links should be ignored
                int index = url.IndexOf("?");
                if (index != -1)
                    url = url.Substring(0, index + 1); // if a link has getdata, remove it
                if (!crawledUrls.Contains(url) && url.StartsWith("http://"))
                {
                    crawledUrls.Add(url); // if a url has not previously been crawled, and is a real link
                    urlsToCrawl.Add(url); // add it to the list of already crawled URLS, and put it in the queue to be crawled
                }
            }
        }
    }
}
