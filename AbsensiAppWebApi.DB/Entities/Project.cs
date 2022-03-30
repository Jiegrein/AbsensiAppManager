using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    [Table("project")]
    public partial class Project
    {
        [Key]
        [Column("project_id")]
        public Guid ProjectId { get; set; }
        [Column("project_name")]
        public string ProjectName { get; set; }
        [Column("blob_id")]
        public Guid? BlobId { get; set; }
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        [Required]
        [Column("created_by")]
        public string CreatedBy { get; set; }
        [Column("updated_at", TypeName = "timestamp with time zone")]
        public DateTime? UpdatedAt { get; set; }
        [Column("updated_by")]
        public string UpdatedBy { get; set; }
        [Column("hour_offset_gmt")]
        public int HourOffsetGmt { get; set; }

        [ForeignKey(nameof(BlobId))]
        [InverseProperty("Projects")]
        public virtual Blob Blob { get; set; }
    }
}
