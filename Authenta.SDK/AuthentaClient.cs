using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Authenta.SDK.Exceptions;

namespace Authenta.SDK
{
    public class AuthentaClient
    {
        private readonly AuthentaHttpClient _http;

        public AuthentaClient(AuthentaOptions options)
        {
            _http = new AuthentaHttpClient(options);
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


            var createRequest1 = new 
            {
                Name = "test",
                ContentType = "image/png",
                Size = 96,
                ModelType = "AC-1"
            };

            // Step 1: create media (JSON)
            var meta = await _http.PostAsync<MediaCreateResponse>("/api/media", createRequest1.ToString());

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
