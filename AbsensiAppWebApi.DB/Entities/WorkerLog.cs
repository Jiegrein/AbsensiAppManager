using System;
using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class WorkerLog
    {
        public string LogId { get; set; }
        public Guid? WorkerId { get; set; }
        public DateTime? StartWork { get; set; }
        public DateTime? StartBreak { get; set; }
        public DateTime? EndBreak { get; set; }
        public DateTime? EndWork { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        public virtual Worker Worker { get; set; }
    }
}
