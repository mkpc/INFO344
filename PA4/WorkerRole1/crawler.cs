using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WorkerRole1
{
    class crawler
    {
        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
        ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static HashSet<string> crawled = new HashSet<string>();
        private static HashSet<string> disallowed = new HashSet<string>();
        private static DateTime lastday = new DateTime(2015, 4, 1);
        private int deQCounter = 0;

        public void readRobots(string url)
        {
            string domain = " ";


            CloudQueueClient queueClientForSiteMap = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForSiteMap = queueClientForSiteMap.GetQueueReference("sitemapurl");
            queueForSiteMap.CreateIfNotExists();

            WebClient client = new WebClient();
            Stream stream = client.OpenRead(url);

            if (url.Contains("www.cnn.com"))
            {
                domain = "http://www.cnn.com";
            }
            else if (url.Contains("bleacherreport"))
            {
                domain = "http://bleacherreport.com";

            }
            using (StreamReader reader = new StreamReader(stream))
            {
                string urlString;
                while ((urlString = reader.ReadLine()) != null)
                {
                    //line = reader.ReadLine();
                    if (urlString.Contains("Sitemap:"))
                    {
                        if (urlString.Contains("http://bleacherreport.com/sitemap/nba.xml"))
                        {
                            urlString = urlString.Replace("Sitemap: ", "");
                            CloudQueueMessage message = new CloudQueueMessage(urlString);
                            queueForSiteMap.AddMessage(message);
                        }
                        else if (
                          urlString.Contains("http://www.cnn.com/sitemaps/"))
                        {
                            urlString = urlString.Replace("Sitemap: ", "");
                            CloudQueueMessage message = new CloudQueueMessage(urlString);
                            queueForSiteMap.AddMessage(message);
                        }
                    }
                    else if (urlString.Contains("Disallow"))
                    {
                        urlString = urlString.Replace("Disallow: ", "");
                        urlString = domain + urlString;
                        disallowed.Add(urlString);
                    }
                }
                reader.Close();
            }
        }

        public void loading(string urlString, CloudQueue queue, string domain)
        {
            if (urlString.Contains(".xml"))
            {
                urlString = cutURL(urlString);
                try
                {
                    //read xml content and save the whole xml as list<string>
                    WebClient client = new WebClient();
                    Stream stream = client.OpenRead(urlString);
                    List<string> content = new List<string>();
                    List<string> xmllink = new List<string>();

                    //var source = client.DownloadString(urlString);
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            content.Add(line);
                        }
                        reader.Close();
                    }

                    if (domain == "CNN")
                    {
                        xmllink = content.Where(x => x.Contains("http://www.cnn.com")).ToList();
                    }
                    else
                    {
                        xmllink = content.Where(x => x.Contains("http://bleacherreport.com/")).ToList();
                    }

                    //read line from List<string>
                    foreach (string lineFromContent in xmllink)
                    {
                        if (checkDate(lineFromContent))//check lastMod date
                        {
                            if (lineFromContent.Contains(".xml"))
                            {
                                loading(lineFromContent, queue, domain);
                            }
                            else//getting a url!
                            {
                                var newline = cutURL(lineFromContent);//trim url
                                CloudQueueMessage message = new CloudQueueMessage(newline);
                                queue.AddMessage(message);

                            }
                        }
                    }
                }
                catch (Exception we)
                {
                    storeERROR(we, urlString);
                }
            }
        }

        private bool isDisallow(string url)
        {
            if (disallowed.Count == 0)
            {
                return false;
            }
            bool result = true;
            foreach (string disallow in disallowed)
            {
                if (!url.StartsWith(disallow))
                {
                    result = false;
                }
                else
                {
                    return true;
                }
            }
            return result;
        }

        public void crawlingHelper(string url, CloudQueue queue, CloudTable table, string domain)
        {
            deQCounter++;
            try
            {
                List<string> htmlList = new List<string>();
                List<string> bodyURLlist = new List<string>();
                string body;
                string title;
                string date;
                string head;
                List<string> link = new List<string>();
                List<string> lastdayList = new List<string>();
                table.CreateIfNotExists();

                if (!crawled.Contains(url) && !isDisallow(url))
                {
                    crawled.Add(url);
                    //find more
                    using (WebClient x = new WebClient())
                    {
                        x.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)");
                        var source = x.DownloadString(url);

                        body = Regex.Match(source, @"\<body\b[^>]*\>\s*(?<Body>[\s\S]*?)\</body\>", RegexOptions.IgnoreCase).Groups["Body"].Value;
                        bodyURLlist = body.Split(new char[] { '"' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        title = Regex.Match(source, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;

                        head = Regex.Match(source, @"\<head\b[^>]*\>\s*(?<Head>[\s\S]*?)\</head\>", RegexOptions.IgnoreCase).Groups["Head"].Value;
                        var lastdateList = head.Split(new string[] { "><" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        lastdayList = lastdateList.Where(line => line.StartsWith("meta content=") && line.EndsWith("name=\"lastmod\"")).ToList();
                    }

                    if (lastdayList.Count == 1)
                    {
                        date = cutDate(lastdayList[0]);
                    }
                    else
                    {
                        date = lastday.ToString();
                    }
                    //encode URL
                    string htmlurl = Uri.EscapeDataString(url.ToLower());

                    title = title.ToLower();
                    var titleList = splitTitle(title).Split(' ');

                    //title = Regex.Replace(title, @"[^\w\s]", "").ToLower();
                    //var titleList = title.Split(new Char[] { ' ', '/', '\'', '.', '"', '’', '\'' });
                    
                    foreach (string key in titleList)
                    {
                        if (key != " "||key!="")
                        {
                            //add to the table
                            urlData urlInsert = new urlData(htmlurl, key);
                            urlInsert.Date = date;
                            urlInsert.Title = title;
                            TableOperation insertOperation = TableOperation.Insert(urlInsert);
                            table.Execute(insertOperation);
                        }
                        
                    }

                    if (domain == "CNN")
                    {
                        //only for cnn.com
                        link = bodyURLlist
                           .Where(x => x.StartsWith("http://www.cnn.com")
                               && !x.Contains(".min.js")
                               && !x.EndsWith(".pdf")
                               && !x.EndsWith("index.xml")
                               && !x.Contains("#page-fb-comments")
                               && !x.Contains("html?symb=")).ToList();
                        foreach (string a in link)
                        {
                            var temp = a;
                            temp = temp.Replace(" ", "");
                            if (!crawled.Contains(temp))
                            {
                                CloudQueueMessage message = new CloudQueueMessage(temp);
                                queue.AddMessage(message);
                            }
                        }
                    }
                    if (domain == "NBA")
                    {
                        //only for NBA
                        link = bodyURLlist
                           .Where(x => x.StartsWith("http://bleacherreport.com/")).ToList();
                        foreach (string a in link)
                        {
                            var temp = a;
                            temp = temp.Replace(" ", "");
                            if (!crawled.Contains(temp))
                            {
                                CloudQueueMessage message = new CloudQueueMessage(temp);
                                queue.AddMessage(message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                storeERROR(ex, url);

            }
        }

        private void storeERROR(Exception ex, string url)
        {
            CloudTableClient tableClientForError = storageAccount.CreateCloudTableClient();
            CloudTable tableForError = tableClientForError.GetTableReference("errormsg");

            try
            {
                tableForError.CreateIfNotExists();
                var message = ex;
                string target = ex.TargetSite.ToString();
                string ERRORmessage = ex.Message;
                int ERRORcode = ex.HResult;
                string getDate = DateTime.Now.ToString();
                url = Uri.EscapeDataString(url);
                errorMessage errorMsg = new errorMessage(ERRORmessage, url);
                errorMsg.errordate = getDate;
                errorMsg.target = target;
                TableOperation insertOperation = TableOperation.Insert(errorMsg);
                tableForError.Execute(insertOperation);
            }
            catch (Exception)
            {

            }
        }

        private string cutDate(string line)
        {
            int start = line.IndexOf("meta content=");
            int end = line.IndexOf("name=");
            if (start != -1 && end != -1)
            {
                start += 14;
                end -= 3;
                line = line.Substring(0, end);
                line = line.Substring(start, line.Length - start);
            }

            return line;
        }

        private bool checkDate(string line)
        {
            int openIndex = line.IndexOf("<lastmod>");
            int closeIndex = line.IndexOf("</lastmod>");
            int openDate = line.IndexOf("<news" + ":publication_date>");
            int closeDate = line.IndexOf("</news:" + "publication_date>");

            if (openIndex == -1 && closeIndex == -1 && openDate == -1 && closeDate == -1)
            {
                return true;
            }

            if (openIndex != -1 && closeIndex != -1)
            {
                openIndex += 9;
                line = line.Substring(0, closeIndex);
                line = line.Substring(openIndex, line.Length - openIndex);
                string ne = line;
                DateTime dt = DateTime.Parse(line);
                return dt < lastday ? false : true;
            }
            else if (openDate != -1 && closeDate != -1)
            {
                openDate += 23;
                line = line.Substring(0, closeDate);
                line = line.Substring(openDate, line.Length - openDate);
                string test = line;
                DateTime dt = DateTime.Parse(line);
                return dt < lastday ? false : true;
            }
            else
            {
                return true;
            }
        }

        private string cutURL(string url)
        {
            int httpIndex = url.IndexOf("http");
            int xmlIndex = url.IndexOf("</loc>");
            if (httpIndex != -1 && xmlIndex != -1)
            {
                url = url.Substring(0, xmlIndex);
                url = url.Substring(httpIndex, url.Length - httpIndex);
            }
            return url;
        }

        public void resetCounter()
        {
            deQCounter = 0;
        }
        public void clearAll()
        {

            crawled.Clear();
            disallowed.Clear();
        }

        public int getCrawledSize()
        {
            return crawled.Count;
        }

        public int getdeQCounter()
        {
            return deQCounter;
        }

        public int gethtmlQSize()
        {
            try
            {
                CloudQueueClient queueClientForHtml = storageAccount.CreateCloudQueueClient();
                CloudQueue queueForHtml = queueClientForHtml.GetQueueReference("htmlurl");
                queueForHtml.FetchAttributes();
                int size = (int)queueForHtml.ApproximateMessageCount;
                return size;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }

        public string splitTitle(string title)
        {
            //List<string> titleTist = new List<string>();
            //string word = "";
            StringBuilder sb = new StringBuilder();
            foreach (char c in title)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == ' ')||(c >= '0' && c <= '9'))
                {
                    sb.Append(c);
                }
            }
           // word = sb.ToString();
           // titleTist.Add(sb.ToString().Split(' '));


            return sb.ToString();    
        }
 

    }
}
