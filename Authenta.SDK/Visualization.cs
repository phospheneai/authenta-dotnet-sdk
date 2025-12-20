using Authenta.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OpenCvSharp;

namespace Authenta.SDK
{
    public static class Visualization
    {
        private static readonly HttpClient _http = new HttpClient();

        // -------------------------
        // Utilities
        // -------------------------

        private static void EnsureDir(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        // -------------------------
        // Heatmap helpers
        // -------------------------

        /// <summary>
        /// Saves image heatmap (PNG/JPEG/etc) as-is.
        /// Matches Python SDK behavior.
        /// </summary>
        public static async Task<string> SaveHeatmapImageAsync(IDictionary<string, object> media,string outPath)
        {
            if (!media.ContainsKey("heatmapURL"))
                throw new InvalidOperationException("No heatmapURL found in media");

            var url = media["heatmapURL"]?.ToString();
            if (string.IsNullOrEmpty(url))
                throw new InvalidOperationException("heatmapURL is empty");

            var resp = await _http.GetAsync(url);
            if ((int)resp.StatusCode == 404)
                throw new InvalidOperationException("Heatmap not available (404)");

            resp.EnsureSuccessStatusCode();

            var bytes = await resp.Content.ReadAsByteArrayAsync();
            EnsureDir(outPath);
            File.WriteAllBytes(outPath, bytes);   // netstandard2.0 safe

            return outPath;
        }

        
		
		public static async Task<string> SaveHeatmapImageExAsync(MediaStatusResponse media,string outPath){
			if (media == null){
				throw new ArgumentNullException(nameof(media));
			}
			// Convert internally (no external adapter needed)
			var dict = new Dictionary<string, object>();

			if (media.Heatmap is string heatmapUrl &&!string.IsNullOrWhiteSpace(heatmapUrl))
			{
				dict["heatmapURL"] = heatmapUrl;
			}

			if (dict.Count == 0){
				throw new InvalidOperationException("No heatmap available for this media.");
			}
			// Delegate to existing implementation
			return await SaveHeatmapImageAsync(dict, outPath);
		}
	
		
		/// <summary>
        /// Saves participant heatmap videos.
        /// </summary>
        public static async Task<List<string>> SaveHeatmapVideoAsync(
            IDictionary<string, object> media,
            string outDir,
            string baseName = "heatmap")
        {
            if (!media.ContainsKey("participants"))
                throw new InvalidOperationException("No participants found in media");

            var participants = media["participants"] as JArray;
            if (participants == null || participants.Count == 0)
                throw new InvalidOperationException("Participants list is empty");

            Directory.CreateDirectory(outDir);
            var outputs = new List<string>();

            for (int i = 0; i < participants.Count; i++)
            {
                var heatmapUrl = participants[i]?["heatmap"]?.ToString();
                if (string.IsNullOrEmpty(heatmapUrl))
                    continue;

                var resp = await _http.GetAsync(
                    heatmapUrl,
                    HttpCompletionOption.ResponseHeadersRead);

                if ((int)resp.StatusCode == 403 ||
                    (int)resp.StatusCode == 404)
                    continue;

                resp.EnsureSuccessStatusCode();

                var dest = Path.Combine(outDir, $"{baseName}_p{i}.mp4");
                using (var fs = File.Create(dest))
                {
                    await resp.Content.CopyToAsync(fs);
                }

                outputs.Add(dest);
            }

            return outputs;
        }

        /// <summary>
        /// Auto-detects media type and saves heatmap.
        /// </summary>
        public static async Task<object> SaveHeatmapAsync(
            IDictionary<string, object> media,
            string outPath,
            string modelType = null)
        {
            if (!string.IsNullOrEmpty(modelType))
            {
                modelType = modelType.ToUpperInvariant();
                if (modelType.StartsWith("AC-"))
                    return await SaveHeatmapImageAsync(media, outPath);
                if (modelType.StartsWith("DF-"))
                    return await SaveHeatmapVideoAsync(media, outPath);
            }

            var type = media.ContainsKey("type")
                ? media["type"]?.ToString()?.ToLowerInvariant()
                : null;

            if (type == "image")
                return await SaveHeatmapImageAsync(media, outPath);

            return await SaveHeatmapVideoAsync(media, outPath);
        }

        // -------------------------
        // Bounding box helpers
        // -------------------------

        public static void DrawBoundingBoxes(
            string videoPath,
            Dictionary<int, List<BoundingBoxItem>> sequenceDict,
            string resultVideoPath)
        {
            using (var cap = new VideoCapture(videoPath))
            {
                if (!cap.IsOpened())
                    throw new InvalidOperationException("Failed to open source video");

                var width = (int)cap.Get(VideoCaptureProperties.FrameWidth);
                var height = (int)cap.Get(VideoCaptureProperties.FrameHeight);
                var fps = cap.Get(VideoCaptureProperties.Fps);
                if (fps <= 0) fps = 25;

                EnsureDir(resultVideoPath);

                using (var writer = new VideoWriter(
                    resultVideoPath,
                    FourCC.MP4V,
                    fps,
                    new OpenCvSharp.Size(width, height)))
                {
                    int frameIndex = 0;
                    using (var frame = new Mat())
                    {
                        while (cap.Read(frame))
                        {
                            List<BoundingBoxItem> items;
                            if (sequenceDict.TryGetValue(frameIndex, out items))
                            {
                                foreach (var item in items)
                                {
                                    var color = item.Class == "real"
                                        ? Scalar.Green
                                        : Scalar.Red;

                                    var b = item.Data;
                                    var xmin = (int)(b[0] * 2);
                                    var ymin = (int)(b[1] * 2);
                                    var xmax = (int)(b[2] * 2);
                                    var ymax = (int)(b[3] * 2);

                                    Cv2.Rectangle(
                                        frame,
                                        new Point(xmin, ymin),
                                        new Point(xmax, ymax),
                                        color,
                                        2);

                                    Cv2.PutText(
                                        frame,
                                        item.Class + " " +
                                        Math.Round(item.Confidence * 100, 2) + "%",
                                        new Point(xmin, ymin - 10),
                                        HersheyFonts.HersheySimplex,
                                        0.9,
                                        color,
                                        2);
                                }
                            }

                            writer.Write(frame);
                            frameIndex++;
                        }
                    }
                }
            }
        }

        // -------------------------
        // Authenta result adapter
        // -------------------------

        public static async Task<Dictionary<int, List<BoundingBoxItem>>>
            AuthentaToSequenceDictAsync(
                IDictionary<string, object> media,
                string defaultClass = "fake",
                double defaultConfidence = 1.0)
        {
            var resultUrl = media["resultURL"]?.ToString();
            if (string.IsNullOrEmpty(resultUrl))
                throw new InvalidOperationException("Missing resultURL");

            var json = await _http.GetStringAsync(resultUrl);
            var detail = JObject.Parse(json);

            var bboxDict = detail["boundingBoxes"]?["0"]?["boundingBox"] as JObject;
            if (bboxDict == null)
                throw new InvalidOperationException("Bounding box data missing");

            var sequence = new Dictionary<int, List<BoundingBoxItem>>();

            foreach (var prop in bboxDict.Properties())
            {
                var frameIdx = int.Parse(prop.Name);
                var coords = prop.Value.ToObject<double[]>();

                var item = new BoundingBoxItem
                {
                    Data = coords,
                    Class = defaultClass,
                    Confidence = defaultConfidence
                };

                if (!sequence.ContainsKey(frameIdx))
                    sequence[frameIdx] = new List<BoundingBoxItem>();

                sequence[frameIdx].Add(item);
            }

            return sequence;
        }

        public static async Task<string> SaveBoundingBoxVideoAsync(
            IDictionary<string, object> media,
            string srcVideoPath,
            string outVideoPath)
        {
            var seq = await AuthentaToSequenceDictAsync(media);
            DrawBoundingBoxes(srcVideoPath, seq, outVideoPath);
            return outVideoPath;
        }
		
		
    }
}
