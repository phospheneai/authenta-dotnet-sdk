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

### NuGet (Recommended)

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

