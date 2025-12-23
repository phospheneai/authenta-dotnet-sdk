# Authenta dotnet SDK Documentation

Welcome to the official documentation for the **Authenta dotnet SDK**. This library allows you to integrate state-of-the-art deepfake and manipulated media detection into your dotnet applications.
---
### Supported Platforms

- .NET Standard 2.0
- .NET Framework 4.6.1+
- .NET Core 2.0+
- .NET 6+
- .NET SDK 6.0 or later (for development & tooling)
- Library target: **.NET Standard 2.0**
- Supported OS: Windows, Linux, macOS

---
## Features

- Image deepfake detection (`AC-1`)
- Video deepfake detection (`DF-1`)
- Binary upload using presigned URLs
- Explicit processing control
- Polling with timeout handling
- .NET Standard 2.0 compatible

## 1. Getting Started
## 1.1 Installation
### Via NuGet (Recommended)
```dotnet add package Authenta.SDK```

## 1.2 From Source
```
git clone https://github.com/phospheneai/authenta-dotnet-sdk.git
cd authenta-dotnet-sdk/Authenta.SDK
dotnet restore
dotnet build -c Release
dotnet pack -c Release
```


## Build the SDK

From the repository root:

```bash
cd Authenta.SDK
dotnet restore
dotnet build -c Release
```
## packaging
```bash
cd Authenta.SDK
dotnet restore
dotnet build -c Release
dotnet pack -c Release
```

## Client Development

### Add Reference via NuGet (Recommended)

```bash
dotnet add package Authenta.SDK
```
### Initialization
## Authentication

```csharp
var client = new AuthentaClient(new AuthentaOptions
{
    BaseUrl = "<AUTHENTA_BASE_URL>",
    ClientId = "<CLIENT_ID>",
    ClientSecret = "<CLIENT_SECRET>"
});
```
---

## 2. Models & Capabilities

Authenta provides specialized models for different media types. You select the model using the `model_type` parameter in SDK methods.

| Model Type | Modality | Capability |
| :--- | :--- | :--- |
| **`AC-1`** | Image | **AI-Generated Image Detection:** Identifies images created by Generative AI (e.g., Midjourney, Stable Diffusion) or manipulated via editing tools. |
| **`DF-1`** | Video | **Deepfake Video Detection:** Detects face swaps, reenactments, and other facial manipulations in video content. |

---

## 3. Workflows

### Quick Detection (Synchronous)
Use UploadProcessAndWaitAsync to upload media and wait for the final result in a single blocking call.
This is ideal for scripts, console apps, or simple integrations.

```dotnet c#
using Authenta.SDK;
using Authenta.SDK.Models;

// Initialize client
var options = new AuthentaOptions
{
    BaseUrl = "<AUTHENTA_BASE_URL>",
    ClientId = "<CLIENT_ID>",
    ClientSecret = "<CLIENT_SECRET>"
};

var client = new AuthentaClient(options);

// Example: Detect AI-generated image
MediaStatusResponse media =
    await client.UploadProcessAndWaitAsync("path-of-media/nano_img.png",modelType: "AC-1");

Console.WriteLine($"Media ID : {media.Mid}");
Console.WriteLine($"Status   : {media.Status}");
Console.WriteLine($"Result   : {media.Result}");
```
### Async Upload & Polling
For non-blocking workflows (e.g., web APIs or background services), use a two-step process:
upload first, then poll for status using the Media ID (Mid).
Example implementation can be found in
.net standard framework 4.6.1
[`Program.cs`](https://github.com/phospheneai/authenta-dotnet-sdk/blob/master/Authenta.SDK.Example3/Program.cs).

.net core
[`Program.cs`](https://github.com/phospheneai/authenta-dotnet-sdk/blob/master/Authenta.SDK.Example2/Program.cs).




 
#### 1. Initiate upload
 
```c#

MediaStatusResponse uploadMeta =
    await client.UploadAsync(
        "samples/video.mp4",
        modelType: "DF-1"
    );

var mid = uploadMeta.Mid;
Console.WriteLine($"Upload started. Media ID: {mid}");
```

#### 2. Poll for final status
 
```c#
MediaStatusResponse finalMedia =
    await client.WaitForMediaAsync(mid);

if (finalMedia.Status == "PROCESSED")
{
    Console.WriteLine($"Result: {finalMedia.Result}");
}
```
 
#### 3. List Media
 
```c#
var response = await client.ListMediaAsync(new Dictionary<string, object>
{
    { "limit", 20 },
    { "offset", 0 }
});

Console.WriteLine($"Total  : {response.Total}");
Console.WriteLine($"Limit  : {response.Limit}");
Console.WriteLine($"Offset : {response.Offset}");

foreach (var item in response.Items)
{
    Console.WriteLine($"[{item.ModelType}] {item.Name} - {item.Status}");
}
```
 
#### 4. Get Media Status
 
```c#
var media = await client.GetMediaAsync(mediId);

Console.WriteLine($"Status : {media.Status}");
Console.WriteLine($"Result : {media.Result}");
``` 
#### 5. delete Media
 
```c#
await client.DeleteMediaAsync(mediId);
Console.WriteLine("Media deleted successfully.");
```

Retrieve previously uploaded media with pagination support.

### Visualizing Results
The SDK includes a `visualization` module to generate visual overlays (heatmaps and bounding boxes) to help you interpret detection results.

## Visualization Features

It supports both **image models (AC-*)** and **video models (DF-*)**, including heatmaps and bounding box overlays.

### Key Methods

| Method                          | Purpose                                      | Model Type |
|---------------------------------|----------------------------------------------|------------|
| `SaveHeatmapImageAsync`         | Save single heatmap image                    | AC-1       |
| `SaveHeatmapVideosAsync`        | Save per-participant heatmap videos          | DF-1       |
| `SaveHeatmapAsync`              | Unified: auto-detects and saves correct type | AC-1 / DF-1|
| `SaveBoundingBoxVideoAsync`     | Create video with bounding boxes overlaid    | DF-1       |
| `SaveImageArtefactsAsync`       | Save all artefacts for images                | AC-1       |
| `SaveVideoArtefactsAsync`       | Save all artefacts for videos (recommended)  | DF-1       |

### 3.2.1 Heatmaps (Images – AC‑1)

Generate a visual heatmap indicating manipulated regions for an image.
Example implementation can be found in
[`Program.cs`](https://github.com/phospheneai/authenta-dotnet-sdk/blob/master/Authenta.SDK.heatmap/Program.cs).
```c#
  C#MediaStatusResponse media = await client.UploadProcessAndWaitAsync(
    filePath: "samples/image.png",
    modelType: "AC-1"
);

if (!string.IsNullOrEmpty(media.HeatmapUrl))
{
    string outputPath = "results/image_heatmap.jpg";
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

    await AuthentaVisualization.SaveHeatmapImageAsync(media, outputPath);
    Console.WriteLine($"Heatmap saved: {outputPath}");
}
``` 

This downloads the `heatmapURL` from the API response and saves an RGB overlay image.
### 3.2.2 Video Heatmaps (DF-1) — Per Participant
```c#
MediaStatusResponse media = await client.UploadProcessAndWaitAsync(
    filePath: "samples/video.mp4",
    modelType: "DF-1",
    timeout: TimeSpan.FromMinutes(15)
);

// Important: Heatmap videos are generated AFTER processing completes
// Add delay and refresh media object
await Task.Delay(TimeSpan.FromSeconds(60));
media = await client.GetMediaAsync(media.Mid);

if (media.Participants?.Count > 0)
{
    var paths = await AuthentaVisualization.SaveHeatmapVideosAsync(
        media: media,
        outDir: "results/heatmaps",
        baseName: "participant_heatmap"
    );

    Console.WriteLine($"{paths.Count} participant heatmap video(s) saved:");
    foreach (var path in paths)
        Console.WriteLine($"  → {path}");
}
else
{
    Console.WriteLine("No participants detected or heatmaps not ready yet.");
}
	
```
## 4 Error Handling
4.1 The SDK throws typed exceptions just like the official Python SDK:
```C#
try
{
    var media = await client.UploadProcessAndWaitAsync("file.jpg", "AC-1");
}
catch (AuthenticationException) { Console.WriteLine("Invalid credentials"); }
catch (InsufficientCreditsException) { Console.WriteLine("Out of credits"); }
catch (QuotaExceededException) { Console.WriteLine("Rate limit hit"); }
catch (TimeoutException) { Console.WriteLine("Processing took too long"); }
catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
```

## 5 Examples
### 1 Quick Detection (Synchronous)
```# dotnet core.
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
    }
}
```

### 1 Upload and polling
```# dotnet core
using Authenta.SDK;
using Authenta.SDK.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
public static class MediaStatusExtensions
{
    public static string ToPrettyString(this MediaStatusResponse media)
    {
        return JsonConvert.SerializeObject(
            media,
            Formatting.Indented
        );
    }
}
class Program
{
    static async Task Main()
    {
        Console.WriteLine("Authenta SDK Test Client");

        var baseDir = AppContext.BaseDirectory;
        var imagePath = Path.GetFullPath(Path.Combine(baseDir, "../../../..", "data_samples", "nano_img.png"));
        Console.WriteLine("source data path: " + imagePath);
        var options = new AuthentaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
            ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET")
        };

        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new InvalidOperationException("Authenta credentials are not configured.");
        }
        var client = new AuthentaClient(options);
        var result = await client.UploadFileAsync(imagePath, "AC-1");

        var waitReponse = await client.WaitForMediaAsync(result.Mid, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(3));
        Console.WriteLine(waitReponse.ToPrettyString());
    }
}
```

### 3 list media
```
 using System;
using System.IO;
using System.Threading.Tasks;
using Authenta.SDK;
using Authenta.SDK.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
 

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

        if (string.IsNullOrEmpty(options.ClientId) ||string.IsNullOrEmpty(options.ClientSecret))
        {
            throw new InvalidOperationException("Authenta credentials are not configured.");
        }

         
        var client = new AuthentaClient(options);

        var resp = await client.ListMediaAsync();

        Console.WriteLine($"Total  : {resp.Total}");
        Console.WriteLine($"Limit  : {resp.Limit}");
        Console.WriteLine($"Offset : {resp.Offset}");
        Console.WriteLine();

        if (resp.Data == null || resp.Data.Count == 0)
        {
            Console.WriteLine("No media found.");
            return;
        }

        foreach (var m in resp.Data)
        {
            Console.WriteLine("-----");
            Console.WriteLine($"MID       : {m.Mid}");
            Console.WriteLine($"Name      : {m.Name}");
            Console.WriteLine($"Type      : {m.Type}");
            Console.WriteLine($"Model     : {m.ModelType}");
            Console.WriteLine($"Status    : {m.Status}");
            Console.WriteLine($"CreatedAt : {m.CreatedAt}");

            if (m.Type == "Image")
            {
                Console.WriteLine($"Fake      : {m.Fake}");
                Console.WriteLine($"Confidence: {m.Confidence}");
            }

            if (m.Type == "Video")
            {
                Console.WriteLine($"Faces     : {m.Faces}");
                Console.WriteLine($"DeepFakes : {m.DeepFakes}");
            }
        }
    }
}


```
### 4. Save Heatmap
```
using Authenta.SDK;
using Authenta.SDK.Models;
using OpenCvSharp.Text;
using System;
using System.IO;
using System.Threading;

public class Program
{
    public static async Task Main(string[] args)
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

            string outputRoot = Path.Combine(AppContext.BaseDirectory, "results");
            string caseDir = Path.Combine(outputRoot, media.Mid ?? "unknown");

            if (media.ModelType.StartsWith("AC-"))
            {
                await Visualization.SaveImageArtefactsAsync(media, caseDir, "face");
            }
            else // DF-*
            {
                await Visualization.SaveVideoArtefactsAsync(media, videoPath, caseDir, "scene");
            }

            Console.WriteLine("All visualization artefacts saved successfully!");

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
```

### 5 Save Bounding Box Video
```
using Authenta.SDK;
using Authenta.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class Program
{
    public static async Task Main(string[] args)
    {
        var baseDir = AppContext.BaseDirectory;
        var videoPath = Path.GetFullPath(Path.Combine(baseDir, "../../../..", "data_samples", "val_00000044.mp4"));

        string outputDir = Path.Combine(AppContext.BaseDirectory, "output");
        Directory.CreateDirectory(outputDir);
        string bboxVideoPath = Path.Combine(outputDir, "annotated_with_boxes.mp4");

        if (!File.Exists(videoPath))
        {
            Console.WriteLine($"Error: File not found: {videoPath}");
            return;
        }
        try
        {
            var options = new AuthentaOptions
            {
                BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
                ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
                ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET"),
            };
           
            var client = new AuthentaClient(options);

            // Step 1: Upload + Process → This is REQUIRED
            MediaStatusResponse media = await client.UploadProcessAndWaitAsync(
                filePath: videoPath,
                modelType: "DF-1",                 // or "AC-1" for images (but bounding boxes are usually DF-1)
                pollInterval: TimeSpan.FromSeconds(5),
                timeout: TimeSpan.FromMinutes(10)
            );

            Console.WriteLine($"Processing complete. Status: {media.Status}");
            Console.WriteLine($"Result URL available: {!string.IsNullOrEmpty(media.Result)}");
            string savedPath = await Visualization.SaveBoundingBoxVideoAsync(
                media: media,
                srcVideoPath: videoPath,
                outVideoPath: bboxVideoPath
            );

            Console.WriteLine($"Bounding box video saved: {savedPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No bounding boxes available: {ex.Message}");
        }

    }
}
 
```