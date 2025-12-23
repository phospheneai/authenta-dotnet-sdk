using Authenta.SDK;
using Authenta.SDK.Models;
using OpenCvSharp.Text;
using System;
using System.IO;
using System.Threading;

class Program
{
    static async Task Main(string[] args)
    {
        var options = new AuthentaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
            ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET"),
        };
        var baseDir = AppContext.BaseDirectory;
        var videoPath = Path.GetFullPath(Path.Combine(baseDir, "../../../..", "data_samples", "val_00000044.mp4"));
        if (!File.Exists(videoPath))
        {
            Console.WriteLine($"Error: File not found: {videoPath}");
            return;
        }

        Console.WriteLine("Uploading and processing video (DF-1)... This may take several minutes.");

        try
        {
            var client = new AuthentaClient(options);

            MediaStatusResponse media = await client.UploadProcessAndWaitAsync(
                filePath: videoPath,
                modelType: "DF-1",                    // Use "AC-1" only for images
                pollInterval: TimeSpan.FromSeconds(5),
                timeout: TimeSpan.FromMinutes(15)     // Videos can take 10+ minutes depending on length
            );

            Console.WriteLine($"Processing complete! Status: {media.Status}");
            Console.WriteLine($"Detected faces: {media.Faces ?? 0}");
            Console.WriteLine($"Deepfakes detected: {media.DeepFakes ?? 0}");

            // Optional: Save heatmap videos (may require extra wait time)
            if (media.Participants != null && media.Participants.Count > 0)
            {
                Console.WriteLine("Waiting extra time for heatmap videos to generate...");
                await Task.Delay(TimeSpan.FromSeconds(45)); // Give Authenta time to generate heatmaps

                // Refresh media to get updated heatmap URLs
                media = await client.GetMediaAsync(media.Mid);

                var heatmapPaths = await Visualization.SaveHeatmapVideosAsync(
                    media: media,
                    outDir: "results",
                    baseName: "deepfake_heatmap"
                );

                Console.WriteLine($"{heatmapPaths.Count} heatmap video(s) saved:");
                foreach (var path in heatmapPaths)
                {
                    Console.WriteLine($"   → {path}");
                }
            }
            else
            {
                Console.WriteLine("No participants detected or no heatmap URLs available yet.");
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine("Processing timed out. Try increasing the timeout for longer videos.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}