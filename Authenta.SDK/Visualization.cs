using Authenta.SDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using OpenCvSharp;

namespace Authenta.SDK
{
    /// <summary>
    /// .NET Standard 2.0 compatible visualization helpers
    /// </summary>
    public static class Visualization
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static async Task<string> SaveHeatmapImageAsync(MediaStatusResponse media, string outPath)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            if (string.IsNullOrWhiteSpace(media.Heatmap))
                throw new InvalidOperationException("No heatmapURL found in media (required for AC-1 image model)");

            HttpResponseMessage response = await _httpClient.GetAsync(media.Heatmap);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Heatmap not available (404). The presigned URL may have expired.");

            response.EnsureSuccessStatusCode();

            EnsureDirectory(outPath);

            using (FileStream fileStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            return outPath;
        }

        public static async Task<List<string>> SaveHeatmapVideosAsync(MediaStatusResponse media, string outDir, string baseName = "heatmap")
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            if (media.Participants == null || media.Participants.Count == 0)
                throw new InvalidOperationException("No participants found in media for video heatmaps");

            Directory.CreateDirectory(outDir);

            var savedPaths = new List<string>();

            for (int i = 0; i < media.Participants.Count; i++)
            {
                Participant participant = media.Participants[i];
                if (string.IsNullOrWhiteSpace(participant.Heatmap))
                {
                    Console.WriteLine($"[Warn] No heatmap URL for participant {i}, skipping.");
                    continue;
                }

                try
                {
                    HttpResponseMessage response = await _httpClient.GetAsync(participant.Heatmap);

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Console.WriteLine($"[Warn] Participant {i} heatmap returned {(int)response.StatusCode}, skipping.");
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    string contentType = response.Content.Headers.ContentType?.MediaType ?? "video/mp4";
                    string extension = GetExtensionFromContentType(contentType) ?? ".mp4";

                    string fileName = $"{baseName}_p{i}{extension}";
                    string fullPath = Path.Combine(outDir, fileName);

                    using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }

                    savedPaths.Add(fullPath);
                    Console.WriteLine($"Saved: {fullPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Failed to download heatmap for participant {i}: {ex.Message}");
                }
            }

            return savedPaths;
        }

        private static string GetExtensionFromContentType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
                return null;

            string type = contentType.Split(';')[0].Trim().ToLowerInvariant();

            switch (type)
            {
                case "video/mp4": return ".mp4";
                case "video/webm": return ".webm";
                case "video/avi": return ".avi";
                case "video/quicktime": return ".mov";
                case "image/jpeg": return ".jpg";
                case "image/png": return ".png";
                default: return null;
            }
        }

        public static async Task<object> SaveHeatmapAsync(MediaStatusResponse media, string outPath, string modelType = null)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            bool isImageModel = !string.IsNullOrEmpty(modelType) && modelType.ToUpperInvariant().StartsWith("AC-");
            bool isVideoModel = !string.IsNullOrEmpty(modelType) && modelType.ToUpperInvariant().StartsWith("DF-");

            // Explicit model type override
            if (isImageModel)
                return await SaveHeatmapImageAsync(media, outPath);

            if (isVideoModel)
                return await SaveHeatmapVideosAsync(media, outPath);

            // Auto-detect
            if (!string.IsNullOrWhiteSpace(media.Heatmap))
                return await SaveHeatmapImageAsync(media, outPath);

            if (media.Participants != null && media.Participants.Count > 0)
                return await SaveHeatmapVideosAsync(media, outPath);

            throw new InvalidOperationException("Could not determine heatmap type (no heatmapURL or participants found)");
        }

        // ===================================================================
        // Bounding Box Visualization
        // ===================================================================

        private static void DrawBoundingBoxes(string videoPath, Dictionary<int, List<BoundingBoxItem>> sequenceDict, string outVideoPath)
        {
            using (var capture = new VideoCapture(videoPath))
            {
                if (!capture.IsOpened())
                    throw new InvalidOperationException("Could not open source video: " + videoPath);

                int width = (int)capture.Get(VideoCaptureProperties.FrameWidth);
                int height = (int)capture.Get(VideoCaptureProperties.FrameHeight);
                double fps = capture.Get(VideoCaptureProperties.Fps);
                if (fps <= 0) fps = 30;

                EnsureDirectory(outVideoPath);

                using (var writer = new VideoWriter(outVideoPath, FourCC.MP4V, fps, new Size(width, height)))
                {
                    int frameIndex = 0;
                    using (var frame = new Mat())
                    {
                        while (capture.Read(frame))
                        {
                            if (sequenceDict.TryGetValue(frameIndex, out var boxes))
                            {
                                foreach (var box in boxes)
                                {
                                    var color = box.IsReal ? Scalar.Green : Scalar.Red;

                                    int x1 = (int)(box.BoundingBox[0] * 2);
                                    int y1 = (int)(box.BoundingBox[1] * 2);
                                    int x2 = (int)(box.BoundingBox[2] * 2);
                                    int y2 = (int)(box.BoundingBox[3] * 2);

                                    Cv2.Rectangle(frame, new Point(x1, y1), new Point(x2, y2), color, 2);

                                    string label = $"{(box.IsReal ? "real" : "fake")} {Math.Round(box.Confidence * 100, 1)}%";
                                    Cv2.PutText(frame, label, new Point(x1, y1 - 10), HersheyFonts.HersheySimplex, 0.9, color, 2);
                                }
                            }

                            writer.Write(frame);
                            frameIndex++;
                        }
                    }
                }
            }
        }

        public static async Task<Dictionary<int, List<BoundingBoxItem>>> AuthentaToSequenceDictAsync(MediaStatusResponse media)
        {
            if (media == null)
                throw new ArgumentNullException(nameof(media));

            if (string.IsNullOrWhiteSpace(media.Result))
                throw new InvalidOperationException("Missing resultURL in media");

            string json = await _httpClient.GetStringAsync(media.Result);
            JObject detail = JObject.Parse(json);

            var bboxRoot = detail["boundingBoxes"]?["0"]?["boundingBox"] as JObject;
            if (bboxRoot == null)
                throw new InvalidOperationException("Bounding box data not found in result JSON");

            var sequence = new Dictionary<int, List<BoundingBoxItem>>();

            foreach (var prop in bboxRoot.Properties())
            {
                int frameIndex = int.Parse(prop.Name);
                double[] coords = prop.Value.ToObject<double[]>();

                var item = new BoundingBoxItem
                {
                    BoundingBox = coords,
                    IsReal = false,           // Authenta defaults to fake unless specified
                    Confidence = 1.0
                };

                if (!sequence.ContainsKey(frameIndex))
                    sequence[frameIndex] = new List<BoundingBoxItem>();

                sequence[frameIndex].Add(item);
            }

            return sequence;
        }

        public static async Task<string> SaveBoundingBoxVideoAsync(MediaStatusResponse media, string srcVideoPath, string outVideoPath)
        {
            var sequence = await AuthentaToSequenceDictAsync(media);
            DrawBoundingBoxes(srcVideoPath, sequence, outVideoPath);
            return outVideoPath;
        }

        // ===================================================================
        // Artefact Savers (high-level convenience methods)
        // ===================================================================

        public static async Task<Dictionary<string, string>> SaveImageArtefactsAsync(MediaStatusResponse media, string outDir, string baseName = "image")
        {
            Directory.CreateDirectory(outDir);

            string heatmapPath = Path.Combine(outDir, $"{baseName}_heatmap.jpg");
            await SaveHeatmapImageAsync(media, heatmapPath);

            return new Dictionary<string, string>
            {
                { "heatmap", heatmapPath }
            };
        }

        public static async Task<Dictionary<string, object>> SaveVideoArtefactsAsync(
            MediaStatusResponse media,
            string srcVideoPath,
            string outDir,
            string baseName = "video")
        {
            Directory.CreateDirectory(outDir);

            // Heatmap videos
            string heatmapBase = $"{baseName}_heatmap";
            List<string> heatmapPaths = await SaveHeatmapVideosAsync(media, outDir, heatmapBase);

            // Bounding box video
            string bboxPath = Path.Combine(outDir, $"{baseName}_bbox.mp4");
            await SaveBoundingBoxVideoAsync(media, srcVideoPath, bboxPath);

            return new Dictionary<string, object>
            {
                { "heatmap", heatmapPaths },
                { "bbox_video", bboxPath }
            };
        }
    }

    // Helper class for bounding box items (add this if not already in Models)
    public class BoundingBoxItem
    {
        public double[] BoundingBox { get; set; }  // [x1, y1, x2, y2] normalized?
        public bool IsReal { get; set; }
        public double Confidence { get; set; }
    }
}