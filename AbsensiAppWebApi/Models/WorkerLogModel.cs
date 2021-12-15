using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbsensiAppWebApi.Models
{
    public class WorkerLogModel
    {
        public string WorkerId { get; set; }
        public string ProjectId { get; set; }
        public int ScanEnumId { get; set; }
    }
}
