using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    [Table("worker")]
    public partial class Worker
    {
        public Worker()
        {
            WorkerLogs = new HashSet<WorkerLog>();
        }

        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("fullname")]
        public string Fullname { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("work_status")]
        public bool? WorkStatus { get; set; }
        [Column("break_status")]
        public bool? BreakStatus { get; set; }
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at", TypeName = "timestamp with time zone")]
        public DateTime? UpdatedAt { get; set; }

        [InverseProperty(nameof(WorkerLog.Worker))]
        public virtual ICollection<WorkerLog> WorkerLogs { get; set; }
    }
}
