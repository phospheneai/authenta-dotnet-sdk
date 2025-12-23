using Authenta.SDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Authenta.SDK
{
    /// <summary>
    /// .NET Standard 2.0 compatible visualization helpers
    /// Equivalent to visualization.py in the official Python SDK
    /// </summary>
    public static class Visualization
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        private static void EnsureDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Saves the single heatmap image for AC-1 models (equivalent to save_heatmap_image in Python)
        /// </summary>
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

        /// <summary>
        /// Saves heatmap videos for each participant in DF-1 models
        /// </summary>
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

        /// <summary>
        /// Helper: maps common MIME types to file extensions (no built-in MimeTypes in .NET Standard 2.0)
        /// </summary>
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

        // You can add SaveImageArtefacts / SaveVideoArtefacts here if needed
        // They would just call the above methods with proper paths
    }
}