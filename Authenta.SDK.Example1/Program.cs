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

        var videoPath = "data_samples\\val_00000044-dottest.mp4"; //adjust the path.
        var imagePath = "data_samples\\nano_img.png"; //adjust the path.

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

        var outputDir = "C:/DG-client-project/output1";
        Directory.CreateDirectory(outputDir);

        var vizDict = MediaAdapters.ToVisualizationDict(media);

         
        try
        {
            var heatmapResult = await Visualization.DownloadHeatmapAsync(vizDict, media.ModelType);

            if (heatmapResult is byte[] imageBytes)
            {
                string imagePathOutput = Path.Combine(outputDir, "heatmap.png");
                Directory.CreateDirectory(outputDir);
                await File.WriteAllBytesAsync(imagePathOutput, imageBytes);
                Console.WriteLine($"Heatmap image saved: {imagePathOutput}");
            }
            else if (heatmapResult is List<string> videoPaths)
            {
                Console.WriteLine($"Heatmap videos saved ({videoPaths.Count}):");
                foreach (var p in videoPaths) Console.WriteLine($"  • {p}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to download heatmap: {ex.Message}");
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