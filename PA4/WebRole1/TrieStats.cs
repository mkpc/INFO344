using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    public class TrieStats : TableEntity
    {
        public TrieStats(int totalTitles, string lastTitle)
        {
            this.PartitionKey = "trie";
            this.RowKey = "trierowkey";
            this.totalTitles = totalTitles;
            this.lastTitle = lastTitle;


        }
        public TrieStats() { }
        public string lastTitle { get; set; }
        public int totalTitles { get; set; }
    }
}