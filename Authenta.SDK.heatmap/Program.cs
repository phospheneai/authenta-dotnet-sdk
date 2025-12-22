using System;
using System.IO;
using System.Threading.Tasks;
using Authenta.SDK;
using Authenta.SDK.Models;

class Program
{
    static async Task Main()
    {
        var options = new AuthentaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
            ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET"),
        };

        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
        {
            Console.WriteLine("Please set AUTHENTA_CLIENT_ID and AUTHENTA_CLIENT_SECRET environment variables.");
            return;
        }

        var videoPath = "data_samples\\val_00000044-dottest.mp4";
        var imagePath = "data_samples\\nano_img.png";

        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Source video not found: {imagePath}");
            return;
        }

        var client = new AuthentaClient(options);

        Console.WriteLine("Uploading and processing with DF-1...");
        var media = await client.UploadProcessAndWaitAsync(
            imagePath,
            modelType: "AC-1", // or "AC-1" for images
            pollInterval: TimeSpan.FromSeconds(5),
            timeout: TimeSpan.FromMinutes(5)
        );

        Console.WriteLine($"Done! Status: {media.Status}, Model: {media.ModelType}");

        var outputDir = "output1";
        Directory.CreateDirectory(outputDir);

        var vizDict = MediaAdapters.ToVisualizationDict(media);

        // Save heatmap (image or per-participant videos)
        try
        {
            var result = Path.Combine(outputDir, "heatmap.jpg");

            var heatmapResult = await Visualization.SaveHeatmapAsync(
                vizDict,
                result,
                modelType: media.ModelType // "DF-1" or "AC-1"
            );

            if (heatmapResult is string imgPath)
                Console.WriteLine($"Heatmap image saved: {imgPath}");
            else if (heatmapResult is List<string> vidPaths)
            {
                Console.WriteLine($"Heatmap videos saved ({vidPaths.Count}):");
                foreach (var p in vidPaths) Console.WriteLine($"  • {p}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No heatmap available: {ex.Message}");
        }

        // Save bounding box video
        if (!string.IsNullOrEmpty(media.Result))
        {
            try
            {
                var bboxVideo = Path.Combine(outputDir, "result_with_boxes.mp4");
                await Visualization.SaveBoundingBoxVideoAsync(vizDict, videoPath, bboxVideo);
                Console.WriteLine($"Bounding box video saved: {bboxVideo}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bounding box failed: {ex.Message}");
            }
        }

        Console.WriteLine("All done! Check the 'output' folder.");
    }
}