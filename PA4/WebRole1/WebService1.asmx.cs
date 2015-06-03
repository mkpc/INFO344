using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using System.Xml;
using System.Xml.Linq;

namespace WebRole1
{
    /// <summary>
    /// Summary description for WebService1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class WebService1 : System.Web.Services.WebService
    {

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
        private static Dictionary<string, List<urlData>> cacheDict = new Dictionary<string, List<urlData>>();
        public static Trie tree = new Trie();
        private static string filePath;


        [WebMethod]
        public void reset()
        {

            //Queue for loading sitemap from robots.txt
            CloudQueueClient queueClientForSiteMap = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForSiteMap = queueClientForSiteMap.GetQueueReference("sitemapurl");

            //Queue for loading sitemap 
            CloudQueueClient queueClientForHtml = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForHtml = queueClientForHtml.GetQueueReference("htmlurl");

            CloudQueueClient queueClientForCommand = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForCommand = queueClientForCommand.GetQueueReference("command");

            CloudTableClient tableClientForUrlData = storageAccount.CreateCloudTableClient();
            CloudTable tableForData = tableClientForUrlData.GetTableReference("urldata");

            CloudQueueClient queueClientForRobots = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForRobots = queueClientForCommand.GetQueueReference("robots");

            CloudTableClient tableClientForError = storageAccount.CreateCloudTableClient();
            CloudTable tableForError = tableClientForError.GetTableReference("errormsg");

            queueForRobots.Clear();
            queueForCommand.Clear();
            queueForHtml.Clear();
            queueForSiteMap.Clear();
            tableForData.DeleteIfExists();
            tableForError.DeleteIfExists();

        }

        [WebMethod]
        public void start()
        {
            reset();

            CloudQueueClient queueClientForCommand = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForCommand = queueClientForCommand.GetQueueReference("command");
            queueForCommand.CreateIfNotExists();

            CloudQueueClient queueClientForRobots = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForRobots = queueClientForCommand.GetQueueReference("robots");
            queueForRobots.CreateIfNotExists();



            //send robots.txt address to worker role
            CloudQueueMessage robotsMessage = new CloudQueueMessage("http://www.cnn.com/robots.txt");
            queueForRobots.AddMessage(robotsMessage);

            CloudQueueMessage robotsMessage2 = new CloudQueueMessage("http://bleacherreport.com/robots.txt");
            queueForRobots.AddMessage(robotsMessage2);

            //send start command to worker role
            CloudQueueMessage commandMessage = new CloudQueueMessage("start");
            queueForCommand.AddMessage(commandMessage);
        }


        [WebMethod]
        public void stop()
        {
            CloudQueueClient queueClientForCommand = storageAccount.CreateCloudQueueClient();
            CloudQueue queueForCommand = queueClientForCommand.GetQueueReference("command");
            queueForCommand.CreateIfNotExists();
            CloudQueueMessage stopMessage = new CloudQueueMessage("Stop");
            queueForCommand.AddMessage(stopMessage);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public List<string> searchFromTable(string keyword)
        {
            try
            {
                List<urlData> data = new List<urlData>();
                var keywords = keyword.ToLower().Split(' ');
                List<string> result = new List<string>();



                foreach (string key in keywords)
                {
                    List<urlData> listForCache = new List<urlData>();
                    if (cacheDict.ContainsKey(key))
                    {
                        //cache data contains keyword
                        foreach (urlData u in cacheDict[key])
                        {
                            data.Add(u);
                        }
                    }
                    else
                    {
                        //Cache data don't contains keyword, query the first keyword
                        CloudTableClient tableClientForUrlData = storageAccount.CreateCloudTableClient();
                        CloudTable tableForData = tableClientForUrlData.GetTableReference("urldata");
                        TableQuery<urlData> queryResult = new TableQuery<urlData>()
                        .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, key));
                        foreach (urlData u in tableForData.ExecuteQuery(queryResult))
                        {
                            data.Add(u);
                            listForCache.Add(u);

                        }
                    }
                    if (cacheDict.Count() > 100)
                    {
                        //if the cache over 100 row, then we delete it ramdomly
                        Random rand = new Random();
                        int randNum = rand.Next(100);
                        var remove = cacheDict.Keys.ToList();
                        string removeThis = remove[randNum];
                        cacheDict.Remove(removeThis);
                    }

                    if (data.Count != 0 && !cacheDict.ContainsKey(key))
                    {
                        //save it in the cache
                        cacheDict.Add(key, listForCache);
                    }

                }

                //sort the results
                var findMostFrequent = data
                    .GroupBy(x => x.PartitionKey)
                    .Select(x => new Tuple<string, int, DateTime, urlData>(x.ElementAt(0).Title, x.ToList().Count, Convert.ToDateTime(x.ElementAt(0).Date), x.ElementAt(0)))
                    .OrderByDescending(x => x.Item3)
                    .OrderByDescending(x => x.Item2);

                //output 20 results
                foreach (var u in findMostFrequent)
                {
                    if (result.Count() > 40)
                    {
                        break;
                    }
                    result.Add(u.Item4.Title);
                    result.Add(Uri.UnescapeDataString(u.Item4.PartitionKey));
                }
                return result;
            }
            catch (StorageException ex)
            {
                return null;
            }
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string updateStats()
        {
            CloudTableClient tableClientForStats = storageAccount.CreateCloudTableClient();
            CloudTable tableForstats = tableClientForStats.GetTableReference("stats");
            TableOperation retrieveOperation = TableOperation.Retrieve<stats>("worker", "workerrowkey");
            TableResult retrievedResult = tableForstats.Execute(retrieveOperation);
            int cpu = (((stats)retrievedResult.Result).cpu);
            int mem = (((stats)retrievedResult.Result).mem);
            int qsize = (((stats)retrievedResult.Result).htmlQSize);
            int crawledSize = (((stats)retrievedResult.Result).crawledSize);
            string status = (((stats)retrievedResult.Result).status);
            int deqcounter = (((stats)retrievedResult.Result).deqcounter);


            CloudTableClient tableClientForTrie = storageAccount.CreateCloudTableClient();
            CloudTable tableForTrie = tableClientForTrie.GetTableReference("triestats");
            tableForTrie.CreateIfNotExists();
            TableOperation retrieveTrieOperation = TableOperation.Retrieve<TrieStats>("trie", "trierowkey");
            TableResult retrievedTrieResult = tableForTrie.Execute(retrieveTrieOperation);
            TrieStats resultEntity = (TrieStats)retrievedTrieResult.Result;

            int totalTitlesNum = resultEntity.totalTitles;
            string lastTitle = resultEntity.lastTitle;

            string result = "System status : " + status + " || #Discovered HTML : " + qsize + " entries || #Crawled URL : " + deqcounter + " || Index of table : " + crawledSize + " || CPU useage: " + cpu + "/100 || Memory available  : " + mem + "MB <br/>" + "Total # of titles in Trie : " + totalTitlesNum + " || The last title in Trie : " + lastTitle;

            return result;
        }

    

        [WebMethod]
        public void downloadFile()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("wiki");
            CloudBlockBlob blockBlob2 = container.GetBlockBlobReference("title.txt");

            if (container.Exists())
            {
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        filePath = System.IO.Path.GetTempFileName();
                        using (var fileStream = System.IO.File.OpenWrite(filePath))
                        {
                            blob.DownloadToStream(fileStream);
                        }
                    }
                }
            }

        }
        [WebMethod]
        public void buildTrie()
        {
            //filePath = @"C:\Users\marcocheng\AppData\Roaming\title.txt";
            int RealCounter = 0;
            int counter = 0;
            float check;
            string lastTitle = " ";
            int trieCounter = 0;

            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (RealCounter % 100000 == 0)
                        {
                            Thread.Sleep(2000);
                        }
                        RealCounter++;
                        counter++;
                        if (counter == 1000)
                        {
                            counter = 0;
                            check = GetAcailableMBytes();
                            if (check <= 50)
                            {
                                break;
                            }
                        }
                        else
                        {
                            tree.AddTitle(line);
                            trieCounter++;
                            lastTitle = line;
                        }
                    }
                }
            }
            catch (OutOfMemoryException e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
            Trie dump = tree;

            //store the title information to the table
            CloudTableClient tableClientForTrie = storageAccount.CreateCloudTableClient();
            CloudTable tableForTrie = tableClientForTrie.GetTableReference("triestats");
            tableForTrie.CreateIfNotExists();
            TableOperation retrieveOperation = TableOperation.Retrieve<TrieStats>("trie", "trierowkey");
            TableResult retrievedResult = tableForTrie.Execute(retrieveOperation);
            TrieStats updateEntity = (TrieStats)retrievedResult.Result;
            if (updateEntity != null)
            {
                updateEntity.lastTitle = lastTitle;
                updateEntity.totalTitles = trieCounter;

                TableOperation updateOperation = TableOperation.Replace(updateEntity);
                tableForTrie.Execute(updateOperation);
            }
            else
            {
                TrieStats first = new TrieStats(trieCounter, lastTitle);
                TableOperation InsertOperation = TableOperation.Insert(first);
                tableForTrie.Execute(InsertOperation);
            }
        }


        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available Mbytes");
        [WebMethod]
        public float GetAcailableMBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTree(string input)
        {
            input = input.ToLower();
            return new JavaScriptSerializer().Serialize(tree.SearchForPrefix(input));

        }
    }
}




