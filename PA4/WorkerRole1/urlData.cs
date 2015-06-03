using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkerRole1
{

    public class urlData : TableEntity
    {
        public urlData(string url, string keyword)
        {
            this.PartitionKey = url;
            this.RowKey = keyword;

        }

        public urlData() { }

        public string Keyword { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
    }
}