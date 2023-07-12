using System;
using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class Blob
    {
        public Blob()
        {
            Projects = new HashSet<Project>();
        }

        public Guid BlobId { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        public virtual ICollection<Project> Projects { get; set; }
    }
}
