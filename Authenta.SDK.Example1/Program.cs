using System;
using System.IO;
using System.Threading.Tasks;
using Authenta.SDK;
using Authenta.SDK.Models;
using Newtonsoft.Json.Linq;
public static class MediaAdapters
{
    public static IDictionary<string, object> ToVisualizationDict(MediaStatusResponse media)
    {
        var dict = new Dictionary<string, object>();

        // Single heatmap URL (image OR video)
        if (media.Heatmap is string heatmapUrl && !string.IsNullOrEmpty(heatmapUrl))
        {
            dict["heatmapURL"] = heatmapUrl;
            dict["type"] = "image";   // force image-style handling
        }

        // Result URL (for bounding boxes)
        if (!string.IsNullOrEmpty(media.Result))
        {
            dict["resultURL"] = media.Result;
        }

        // Optional metadata
        dict["status"] = media.Status;
        dict["mid"] = media.Mid;

        return dict;
    }
}

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

        if (string.IsNullOrEmpty(options.ClientId) ||
            string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new InvalidOperationException("Authenta credentials are not configured.");
        }

        var baseDir = AppContext.BaseDirectory;
        var videoPath = Path.GetFullPath(
            Path.Combine(baseDir, "../../../..", "data_samples", "val_00000044-dottest.mp4")
        );

        Console.WriteLine("Source data path:");
        Console.WriteLine(videoPath);

        var client = new AuthentaClient(options);

        var media = await client.UploadProcessAndWaitAsync(
            videoPath,
            modelType: "DF-1",
            pollInterval: TimeSpan.FromSeconds(2),
            timeout: TimeSpan.FromMinutes(3)
        );

        var outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(outputDir);
		var heatmapPath = Path.Combine(outputDir, "heatmap");
		if (media.Heatmap != null){
			await Visualization.SaveHeatmapImageExAsync(media,Path.Combine(outputDir, "heatmap"));
		}
		else
		{
			Console.WriteLine("ℹ No heatmap available.");
		}
        // ✅ Correct visualization for DF-1
        //await Visualization.SaveHeatmapAsync(media,outPath: outputDir,modelType: "DF-1");
		/*
		var dict = MediaAdapters.ToVisualizationDict(media);
		if (dict.ContainsKey("heatmapURL")){
			var heatmapPath = Path.Combine(outputDir, "heatmap");
			await Visualization.SaveHeatmapImageAsync(dict, heatmapPath);
			Console.WriteLine("Heatmap saved to: " + heatmapPath);
		}
		else
		{
			Console.WriteLine("ℹ No heatmap available for this media.");
		}
		*/
       
    }
}
