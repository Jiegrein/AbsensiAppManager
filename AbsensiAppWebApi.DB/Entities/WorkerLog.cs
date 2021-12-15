using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    [Table("worker_log")]
    public partial class WorkerLog
    {
        [Key]
        [Column("log_id")]
        public Guid LogId { get; set; }
        [Column("worker_id")]
        public Guid? WorkerId { get; set; }
        [Column("start_work", TypeName = "timestamp with time zone")]
        public DateTime? StartWork { get; set; }
        [Column("start_break", TypeName = "timestamp with time zone")]
        public DateTime? StartBreak { get; set; }
        [Column("end_break", TypeName = "timestamp with time zone")]
        public DateTime? EndBreak { get; set; }
        [Column("end_work", TypeName = "timestamp with time zone")]
        public DateTime? EndWork { get; set; }
        [Column("project_id")]
        public Guid? ProjectId { get; set; }
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        [Required]
        [Column("created_by")]
        public string CreatedBy { get; set; }
        [Column("updated_at", TypeName = "timestamp with time zone")]
        public DateTime? UpdatedAt { get; set; }
        [Column("updated_by")]
        public string UpdatedBy { get; set; }

        [ForeignKey(nameof(WorkerId))]
        [InverseProperty("WorkerLogs")]
        public virtual Worker Worker { get; set; }
    }
}
