using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    [Table("blob")]
    public partial class Blob
    {
        public Blob()
        {
            Projects = new HashSet<Project>();
        }

        [Key]
        [Column("blob_id")]
        public Guid BlobId { get; set; }
        [Column("path")]
        public string Path { get; set; }
        [Column("file_name")]
        public string FileName { get; set; }
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        [Required]
        [Column("created_by")]
        public string CreatedBy { get; set; }

        [InverseProperty(nameof(Project.Blob))]
        public virtual ICollection<Project> Projects { get; set; }
    }
}
