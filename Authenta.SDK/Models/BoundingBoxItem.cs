    namespace Authenta.SDK.Models
    {
        public class BoundingBoxItem
        {
            public double[] Data { get; set; }   // [x1, y1, x2, y2]
            public string Class { get; set; }    // real | fake
            public double Confidence { get; set; }
        }
    }