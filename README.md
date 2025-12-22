# Authenta dotnet SDK Documentation

Welcome to the official documentation for the **Authenta dotnet SDK**. This library allows you to integrate state-of-the-art deepfake and manipulated media detection into your dotnet applications.

---
## Features

- Image deepfake detection (`AC-1`)
- Video deepfake detection (`DF-1`)
- Binary upload using presigned URLs
- Explicit processing control
- Polling with timeout handling
- .NET Standard 2.0 compatible

## 1. Getting Started

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

## Build the SDK

From the repository root:

```bash
cd Authenta.SDK
dotnet restore
dotnet build -c Release
```
##packaging
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

```dotnet c#
using Authenta.SDK;
using Authenta.SDK.Models;

// 1. Initiate upload
MediaStatusResponse uploadMeta =
    await client.UploadAsync(
        "samples/video.mp4",
        modelType: "DF-1"
    );

var mid = uploadMeta.Mid;
Console.WriteLine($"Upload started. Media ID: {mid}");

// ... perform other work ...

// 2. Poll for final status
MediaStatusResponse finalMedia =
    await client.WaitForMediaAsync(mid);

if (finalMedia.Status == "PROCESSED")
{
    Console.WriteLine($"Result: {finalMedia.Result}");
}
// 3. List Media
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

//4. Get Media Status
var media = await client.GetMediaAsync("MEDIA_ID");

Console.WriteLine($"Status : {media.Status}");
Console.WriteLine($"Result : {media.Result}");

//5. Delete Media
await client.DeleteMediaAsync("MEDIA_ID");
Console.WriteLine("Media deleted successfully.");
```

Retrieve previously uploaded media with pagination support.

### Visualizing Results
The SDK includes a `visualization` module to generate visual overlays (heatmaps and bounding boxes) to help you interpret detection results.

### 3.2.1 Heatmaps (Images – AC‑1)

Generate a visual heatmap indicating manipulated regions for an image.
Example implementation can be found in
[`Program.cs`](https://github.com/phospheneai/authenta-dotnet-sdk/blob/master/Authenta.SDK.heatmap/Program.cs).
```c#
  var client = new AuthentaClient(options);
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
```

This downloads the `heatmapURL` from the API response and saves an RGB overlay image.
### 3.2.2 Heatmaps (Deepfake Video – DF‑1, multi‑face)

For DF‑1, the API can return multiple participants (faces) in a single video.
Each participant may have a separate heatmap video.

Example implementation can be found in
[`Program.cs`](https://github.com/phospheneai/authenta-dotnet-sdk/blob/master/Authenta.SDK.heatmap/Program.cs).
```c#
var videoPath = "data_samples\\val_00000044-dottest.mp4";

var client = new AuthentaClient(options);
	var media = await client.UploadProcessAndWaitAsync(
		videoPath,
		modelType: "DF-1", // or "AC-1" for images
		pollInterval: TimeSpan.FromSeconds(5),
		timeout: TimeSpan.FromMinutes(5)
	);
  var vizDict = MediaAdapters.ToVisualizationDict(media);
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
	
```