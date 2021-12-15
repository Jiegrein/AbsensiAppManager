using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace AbsensiAppWebApi.DB.Entities
{
    [Keyless]
    [Table("scan_enum")]
    public partial class ScanEnum
    {
        [Column("id")]
        public int? Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; }
        [Required]
        [Column("created_by")]
        public string CreatedBy { get; set; }
    }
}
