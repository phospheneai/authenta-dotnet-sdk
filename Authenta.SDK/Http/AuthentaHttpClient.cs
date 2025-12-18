using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Authenta.SDK.Exceptions;
namespace Authenta.SDK
{
    internal class AuthentaHttpClient
    {
        private readonly HttpClient _client;

        public AuthentaHttpClient(AuthentaOptions options)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'))
            };

            _client.DefaultRequestHeaders.Add("x-client-id", options.ClientId);
            _client.DefaultRequestHeaders.Add("x-client-secret", options.ClientSecret);
        }

        public async Task<T> PostAsync<T>(string url, object body)
        {
            var json = JsonConvert.SerializeObject(body);
            var content = new StringContent(json, Encoding.UTF8, "Application/json");
            var response = await _client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
                throw new AuthentaApiException(await response.Content.ReadAsStringAsync(),(int)response.StatusCode);

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new AuthentaApiException(await response.Content.ReadAsStringAsync(),(int)response.StatusCode);

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
    }
}
