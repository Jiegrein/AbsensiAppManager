using System;
using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class Project
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public Guid? BlobId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public int HourOffsetGmt { get; set; }

        public virtual Blob Blob { get; set; }
    }
}
