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
```

### Visualizing Results
The SDK includes a `visualization` module to generate visual overlays (heatmaps and bounding boxes) to help you interpret detection results.
