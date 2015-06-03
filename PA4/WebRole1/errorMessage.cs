using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    class errorMessage : TableEntity
    {
        public errorMessage(string error,string url )
        {
            this.PartitionKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
            this.RowKey = url;
            this.error = error;
            
        }
        public errorMessage() { }
        public string target { get; set; }
        public string error { get; set; }
        public string errordate { get; set; }
    }
}
