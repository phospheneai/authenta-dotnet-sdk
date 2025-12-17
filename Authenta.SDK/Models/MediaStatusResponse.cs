using System;
using System.Collections.Generic;
using System.Text;

namespace Authenta.SDK.Models
{
    public class MediaStatusResponse
    {
        public string Mid { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public object Scores { get; set; }
        public object Heatmap { get; set; }
    }
}
