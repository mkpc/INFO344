using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace WorkerRole1
{

    public class workerRoleStatus
    {
        private PerformanceCounter cpuProcess = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available Mbytes");
        private string status;

        public workerRoleStatus()
        {
            status = "Idle";
        }

        public string getStatus()
        {
            return status;
        }
        public void setStatus(string status)
        {
            this.status = status;
        }
        public bool isStop(){
            if(status.Equals("Stop")){
                return true;
            }else{
                return false;
            }
        }

        public int GetAcailableMBytes()
        {
            int memUsage = (int)memProcess.NextValue();
            return memUsage;
        }

        public int GetCPU()
        {
            int cpuUsage = (int)cpuProcess.NextValue();
            return cpuUsage;
        }
    }
}