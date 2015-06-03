using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Text.RegularExpressions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

        private workerRoleStatus statusclass = new workerRoleStatus();
        private crawler start = new crawler();

        private static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);

        private static CloudQueueClient queueClientForSiteMap = storageAccount.CreateCloudQueueClient();
        private static CloudQueue queueForSiteMap = queueClientForSiteMap.GetQueueReference("sitemapurl");
        private static CloudQueueClient queueClientForHtml = storageAccount.CreateCloudQueueClient();
        private static CloudQueue queueForHtml = queueClientForHtml.GetQueueReference("htmlurl");
        private static CloudQueueClient queueClientForCommand = storageAccount.CreateCloudQueueClient();
        private static CloudQueue queueForCommand = queueClientForCommand.GetQueueReference("command");
        private static CloudTableClient tableClientForUrlData = storageAccount.CreateCloudTableClient();
        private static CloudTable tableForData = tableClientForUrlData.GetTableReference("urldata");
        private static CloudTableClient tableClientForstatus = storageAccount.CreateCloudTableClient();
        private static CloudTable tableForstatus = tableClientForUrlData.GetTableReference("status");
        private static CloudQueueClient queueClientForRobots = storageAccount.CreateCloudQueueClient();
        private static CloudQueue queueForRobots = queueClientForRobots.GetQueueReference("robots");
        private static CloudTableClient tableClientForError = storageAccount.CreateCloudTableClient();
        private static CloudTable tableForError = tableClientForError.GetTableReference("errormsg");
        private static CloudTableClient tableClientForStats = storageAccount.CreateCloudTableClient();
        private static CloudTable tableForstats = tableClientForStats.GetTableReference("stats");





        public override void Run()
        {

            Trace.TraceInformation("WorkerRole1 is running");

            queueForSiteMap.CreateIfNotExists();
            queueForHtml.CreateIfNotExists();
            queueForCommand.CreateIfNotExists();
            //tableForstatus.DeleteIfExists();

            queueForRobots.CreateIfNotExists();
            tableForstats.CreateIfNotExists();
            queueForCommand.Clear();

            //crawler start = new crawler();
            string head = "test";
            bool Loading = false;
            bool Crawling = false;
            bool goReadRobot = true;
            bool isfirst = true;

            while (true)
            {
                CloudQueueMessage commandmessage = queueForCommand.GetMessage();//get command
                if (commandmessage != null)
                {
                    //updateStatus(queueForCommand);
                    string command = commandmessage.AsString;

                    if (command == "start")
                    {
                        
                        // crawler start = new crawler();
                         //start.clearAll();
                        //queueForRobots.Clear();
                        tableForError.DeleteIfExists();
                        clearStats();
                        statusclass.setStatus("ReadRobots");
                        start.clearAll();
                        start.resetCounter();
                        Loading = false;
                        isfirst = true;
                        Crawling = false;
                        goReadRobot = true;
                        //updateStatus(queueForCommand, "ReadRobots");
                        updateStats(isfirst);
                    }
                    try
                    {
                        queueForCommand.DeleteMessage(commandmessage);
                    }catch(Exception)
                    {

                    }                   
                    isfirst = false;
                    //go when readRobot is true and isKill is false
                    while (goReadRobot && statusclass.getStatus().Equals("ReadRobots") && !statusclass.isStop())
                    {
                        //CloudQueueMessage peek = queueForRobots.PeekMessage();
                        while (queueForRobots.PeekMessage() != null)
                        {
                            CloudQueueMessage robotmessage = queueForRobots.GetMessage();
                            string robot = robotmessage.AsString;
                            start.readRobots(robot);
                            queueForRobots.DeleteMessage(robotmessage);
                            updateStats(isfirst);
                        }
                        if (!statusclass.getStatus().Equals("Stop"))
                        {
                            goReadRobot = false;
                            Loading = true;
                            statusclass.setStatus("Loading");
                            updateStatus(queueForCommand, "Loading");
                            updateStats(isfirst);
                        }
                    }

                    //Loading stage
                    while (Loading && statusclass.getStatus().Equals("Loading") && !statusclass.isStop())
                    {
                        updateStatus(queueForCommand, "Loading");
                        while (queueForSiteMap.PeekMessage() != null && statusclass.getStatus().Equals("Loading") && !statusclass.isStop())
                        {
                            CloudQueueMessage sitmapMessage = queueForSiteMap.GetMessage();
                            string sitemap = sitmapMessage.AsString;
                            if (sitemap.Contains("cnn.com"))
                            {
                                head = "CNN";
                            }
                            else
                            {
                                head = "NBA";
                            }
                            start.loading(sitemap, queueForHtml, head);
                            try
                            {

                            queueForSiteMap.DeleteMessage(sitmapMessage);
                            }catch(Exception){

                            }
                            updateStatus(queueForCommand, "Loading");
                            updateStats(isfirst);
                        }
                        if (!statusclass.getStatus().Equals("Stop"))
                        {
                            tableForError.CreateIfNotExists();
                            statusclass.setStatus("Crawling");
                            Loading = false;
                            Crawling = true;
                            updateStatus(queueForCommand, "Crawling");
                            updateStats(isfirst);
                        }
                    }

                    //Crawling stage
                    while (Crawling && statusclass.getStatus().Equals("Crawling") && !statusclass.isStop())
                    {

                        tableForData.CreateIfNotExists();
                        updateStatus(queueForCommand, "Crawling");
                        while (queueForHtml.PeekMessage() != null && statusclass.getStatus().Equals("Crawling") && !statusclass.isStop())
                        {
                            CloudQueueMessage htmlmessage = queueForHtml.GetMessage();
                            string html = htmlmessage.AsString;
                            if (html.Contains("www.cnn.com"))
                            {
                                head = "CNN";
                            }
                            else
                            {
                                head = "NBA";
                            }
                            start.crawlingHelper(html, queueForHtml, tableForData, head);
                            queueForHtml.DeleteMessage(htmlmessage);
                            updateStatus(queueForCommand, "Crawling");
                            updateStats(isfirst);
                        }
                        if (!statusclass.getStatus().Equals("Stop"))
                        {
                            statusclass.setStatus("Idle");
                            Crawling = false;
                            updateStatus(queueForCommand, "Idle");
                            updateStats(isfirst);
                        }
                    }
                }
                updateStatus(queueForCommand, "Idle");
                updateStats(isfirst);
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop()
        {


            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private void updateStatus(CloudQueue queueForCommand, string newStatus)
        {
            CloudQueueMessage commandmessage = queueForCommand.GetMessage();
            if (commandmessage != null)
            {
                string Command = commandmessage.AsString;
                statusclass.setStatus(Command);
               
                if (Command != "start")
                {
                    queueForCommand.DeleteMessage(commandmessage);  
                }
            }
            else
            {
                statusclass.setStatus(newStatus);
            }
        }

        private void updateStats(bool isfirst)
        {
            try
            {

                int mem = statusclass.GetAcailableMBytes();
                int cpu = statusclass.GetCPU();
                int crawledSize = start.getCrawledSize();
                string status = statusclass.getStatus();
                int htmlQSize = start.gethtmlQSize();
                int deqcounter = start.getdeQCounter();

                TableOperation retrieveOperation = TableOperation.Retrieve<stats>("worker", "workerrowkey");
                TableResult retrievedResult = tableForstats.Execute(retrieveOperation);
                stats updateEntity = (stats)retrievedResult.Result;
                if (updateEntity != null)
                {
                    updateEntity.mem = mem;
                    updateEntity.cpu = cpu;
                    updateEntity.crawledSize = crawledSize;
                    updateEntity.status = status;
                    updateEntity.htmlQSize = htmlQSize;
                    updateEntity.deqcounter = deqcounter;

                    TableOperation updateOperation = TableOperation.Replace(updateEntity);
                    tableForstats.Execute(updateOperation);
                }
                else
                {
                    stats first = new stats(cpu, mem, htmlQSize, crawledSize, status, deqcounter);
                    TableOperation InsertOperation = TableOperation.Insert(first);
                    tableForstats.Execute(InsertOperation);
                }
            }
            catch (Exception ex)
            {
                var a = ex;
            }

        }

        private void clearStats()
        {
            TableOperation retrieveOperation = TableOperation.Retrieve<stats>("worker", "workerrowkey");
            TableResult retrievedResult = tableForstats.Execute(retrieveOperation);
            stats deleteEntity = (stats)retrievedResult.Result;
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);
                tableForstats.Execute(deleteOperation);
            }
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}


