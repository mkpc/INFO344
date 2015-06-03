using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WorkerRole1
{
    class stats : TableEntity
    {
        public stats(int cpu, int mem, int htmlQSize, int crawledSize, string newstatus, int deqcounter)
        {
            this.PartitionKey = "worker";
            this.RowKey = "workerrowkey";
            this.cpu = cpu;
            this.mem = mem;
            this.htmlQSize = htmlQSize;
            this.crawledSize = crawledSize;
            this.status = newstatus;
            this.deqcounter = deqcounter;

        }
         public stats() { }

         public string status { get; set; }
         public int cpu { get; set; }
         public int mem { get; set; }
         public int htmlQSize { get; set; }
         public int crawledSize { get; set; }
         public int deqcounter { get; set; }
    }
}
