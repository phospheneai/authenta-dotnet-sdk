using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Authenta.SDK.Models
{
    public class Participant
    {
        [JsonProperty("fake")]
        public bool Fake { get; set; }

        [JsonProperty("confidence")]
        public double Confidence { get; set; }

        [JsonProperty("heatmap")]
        public string Heatmap { get; set; }  // Video URL per participant (DF models)
    }

    public class MediaStatusResponse
    {
        [JsonProperty("mid")]
        public string Mid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }   // "Image" or "Video"

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("modelType")]
        public string ModelType { get; set; }  // e.g. "AC-1", "DF-1"

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("srcURL")]
        public string SrcUrl { get; set; }

        // Common visualization fields
        [JsonProperty("heatmapURL")]
        public string Heatmap { get; set; }  // Single image heatmap (AC models)

        [JsonProperty("resultURL")]
        public string Result { get; set; }   // Detailed results JSON (bounding boxes, etc.)

        // Summary / per-media results (may be present)
        [JsonProperty("fake")]
        public bool? Fake { get; set; }

        [JsonProperty("confidence")]
        public double? Confidence { get; set; }

        // Video-specific summary
        [JsonProperty("faces")]
        public int? Faces { get; set; }

        [JsonProperty("deepFakes")]
        public int? DeepFakes { get; set; }

        // Video models (DF-*): per-participant details
        [JsonProperty("participants")]
        public List<Participant> Participants { get; set; }
    }
}