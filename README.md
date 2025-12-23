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
using Authenta.SDK.Exceptions;
using Authenta.SDK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Authenta.SDK
{
    public class AuthentaClient
    {
        private readonly AuthentaHttpClient _http;

        public AuthentaClient(AuthentaOptions options)
        {
            _http = new AuthentaHttpClient(options);
        }
        public async Task<MediaCreateResponse> CreateMediaAsync(MediaCreateRequest body)
        {
           // var json = JsonConvert.SerializeObject(body);
           // var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync<MediaCreateResponse>("/api/media", body);
            return response;
        }
        public async Task<MediaCreateResponse> UploadFileAsync(string filePath,string modelType)
        {
            if (!File.Exists(filePath))
                throw new AuthentaException("File not found");

            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Length <= 0)
                throw new AuthentaException("File size must be greater than zero");

            if (modelType != "DF-1" && modelType != "AC-1" && modelType != "FD-1")
            {
                throw new AuthentaException("modelType must be one of: DF-1, AC-1, FD-1");
            }

            var mimeType = MimeTypeHelper.GetMimeType(filePath);

            var createRequest = new MediaCreateRequest
            {
                name = SanitizeName( Path.GetFileNameWithoutExtension(fileInfo.Name)),
                contentType = mimeType,
                size = fileInfo.Length,
                modelType = modelType
            };


             

            // Step 1: create media (JSON)
            var meta = await _http.PostAsync<MediaCreateResponse>("/api/media", createRequest);

            if (string.IsNullOrEmpty(meta.UploadUrl))
                throw new AuthentaException("UploadUrl missing in API response");

            // Step 2: upload raw binary (NOT JSON)
            using (var fs = File.OpenRead(filePath))
            using (var content = new StreamContent(fs))
            {
                content.Headers.ContentType =new MediaTypeHeaderValue(mimeType);

                using (var client = new HttpClient())
                {
                    var putResp = await client.PutAsync(meta.UploadUrl,content);

                    if (!putResp.IsSuccessStatusCode)
                    {
                        throw new AuthentaApiException("Binary upload failed",(int)putResp.StatusCode);
                    }
                }
            }

            return meta;
        }
        public async Task ProcessMediaAsync(string mid)
        {
            await _http.PostAsync<object>($"/api/media/{mid}/process", body: null);
        }

        public async Task<MediaStatusResponse> WaitForMediaAsync(string mid,TimeSpan? interval = null,TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(mid))
                throw new ArgumentException("mid is required", nameof(mid));

            if (!interval.HasValue)
            {
                interval = TimeSpan.FromSeconds(5);
            }

            if (!timeout.HasValue)
            {
                timeout = TimeSpan.FromMinutes(5);
            }

            var start = DateTime.UtcNow;

            while (true)
            {
                var media = await GetMediaAsync(mid);
                var status = (media.Status ?? string.Empty).ToUpperInvariant();

                if (status == "PROCESSED" ||status == "FAILED" || status == "ERROR")
                {
                    return media;
                }

                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException($"Timed out waiting for media {mid}, last status={status}");
                }

                await Task.Delay(interval.Value);
            }
        }
        public async Task<MediaStatusResponse> GetMediaAsync(string mid)
        {
            if (string.IsNullOrWhiteSpace(mid))
                throw new ArgumentException("mid is required", nameof(mid));

            return await _http.GetAsync<MediaStatusResponse>( $"/api/media/{mid}");
        }

        public async Task<MediaStatusResponse> UploadProcessAndWaitAsync(string filePath,string modelType,TimeSpan? pollInterval = null,TimeSpan? timeout = null)
        {
            var fileInfo = new FileInfo(filePath);
            var mimeType = MimeTypeHelper.GetMimeType(filePath);

            var create = await CreateMediaAsync(new MediaCreateRequest
            {
                name = Path.GetFileNameWithoutExtension(filePath),
                contentType = mimeType,
                size = fileInfo.Length,
                modelType = modelType
            });


            // Step 2: upload raw binary (NOT JSON)
            using (var fs = File.OpenRead(filePath))
            using (var content = new StreamContent(fs))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                using (var client = new HttpClient())
                {
                    var putResp = await client.PutAsync(create.UploadUrl, content);

                    if (!putResp.IsSuccessStatusCode)
                    {
                        throw new AuthentaApiException("Binary upload failed", (int)putResp.StatusCode);
                    }
                }
            }
            //await ProcessMediaAsync(create.Mid);

            return await WaitForMediaAsync(create.Mid,pollInterval,timeout);
        }
        public async Task<MediaListResponse> ListMediaAsync(IDictionary<string, string> queryParams = null)
        {
            var url = "/api/media";

            if (queryParams != null && queryParams.Count > 0)
            {
                var qs = string.Join("&",
                    queryParams.Select(kv =>
                        $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
                url += "?" + qs;
            }

            return await _http.GetAsync<MediaListResponse>(url);
        }
		public async Task DeleteMediaAsync(string mid)
        {
            if (string.IsNullOrWhiteSpace(mid))
                throw new ArgumentException("mid cannot be empty", nameof(mid));

            var url = $"/api/media/{mid}";
            await _http.DeleteAsync(url);
        }
        private static string SanitizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "media";

            var clean = Regex.Replace(
                input,
                @"[^a-zA-Z0-9 _-]",
                ""
            );

            return clean.Length > 24
                ? clean.Substring(0, 24)
                : clean;
        }
    }
}

```