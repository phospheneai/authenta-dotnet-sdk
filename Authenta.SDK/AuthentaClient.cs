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
