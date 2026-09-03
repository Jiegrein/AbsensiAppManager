using System;
using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class Worker
    {
        public Worker()
        {
            WorkerLogs = new HashSet<WorkerLog>();
        }

        public Guid Id { get; set; }
        public string Fullname { get; set; }
        public string Name { get; set; }
        public bool? WorkStatus { get; set; }
        public bool? BreakStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal DailyPay { get; set; }
        public string Phone { get; set; }

        public virtual ICollection<WorkerLog> WorkerLogs { get; set; }
    }
}
