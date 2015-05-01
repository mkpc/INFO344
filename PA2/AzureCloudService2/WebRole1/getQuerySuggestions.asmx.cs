using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.IO;
using System.Web.Script.Serialization;
using System.Diagnostics;
using System.Web.Script.Services;

namespace PA2
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
        public static Trie tree = new Trie();
        private static string filePath;

          public void Main(){
              
          }
   

        [WebMethod]
        public void downloadFile()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
            ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("info344pa2");
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
        public  void buildTrie()
        {
            
            int RealCounter = 0;
            int counter = 0;          
            float check;

            try
            {
             
                using ( StreamReader sr = new StreamReader(filePath))
                {
                    string line;
                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        RealCounter++;
                        counter++;
                        if (counter==1000)
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
                        }
                    }
                }

            }
            catch (OutOfMemoryException e)
            {
                Console.WriteLine("{0} Exception caught.", e);
            }
            Trie dump = tree;
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

