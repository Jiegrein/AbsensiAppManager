using System;
using System.Collections.Generic;

namespace AbsensiAppWebApi.DB.Entities
{
    public partial class ScanEnum
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }
}
