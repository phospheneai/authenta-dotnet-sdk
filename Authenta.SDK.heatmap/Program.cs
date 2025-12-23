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
        var baseDir = AppContext.BaseDirectory;
        var imagePath = Path.GetFullPath(Path.Combine(baseDir, "../../../..", "data_samples", "nano_img.png"));

     
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"Source video not found: {imagePath}");
            return;
        }

        var client = new AuthentaClient(options);

        Console.WriteLine("Uploading and processing with AC-1...");
        var media = await client.UploadProcessAndWaitAsync(
            imagePath,
            modelType: "AC-1", // or "AC-1" for images
            pollInterval: TimeSpan.FromSeconds(5),
            timeout: TimeSpan.FromMinutes(5)
        );

        await Visualization.SaveHeatmapImageAsync(media, "results/image_heatmap.jpg");


    }
}