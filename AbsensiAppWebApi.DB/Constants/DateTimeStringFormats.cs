using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbsensiAppWebApi.DB.Constants
{
    public class DateTimeStringFormats
    {
        /// <summary>
        /// This constant value will be used for displaying date & time string format on some UIs.
        /// </summary>
        public const string FullDateTime = "dd-MMMM-yyyy hh:mm tt";

        /// <summary>
        /// This constant value will be used for displaying date & time string format on some UIs.
        /// </summary>
        public const string DateTimeString = "dd-MM-yyyy hh:mm tt";

        /// <summary>
        /// This constant value is the default format for CRM
        /// </summary>
        public const string DefaultDate = "dd-MM-yyyy";

        /// <summary>
        /// This constant value is the default format for CRM
        /// </summary>
        public const string FullDate = "dddd, dd MMMM yyyy";
    }
}
