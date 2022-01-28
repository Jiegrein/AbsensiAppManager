using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbsensiAppWebApi.Models
{
    // Use as model when returning CreateWorkerLog
    public class NewLogModel
    {
        public string LogId { get; set; }
        public string ProjectId { get; set; }
    }
}
