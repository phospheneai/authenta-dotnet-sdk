using Authenta.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Example3
{
    internal class Program
    {
        static  void Main(string[] args)
        {
            var baseDir = AppContext.BaseDirectory;
            var imagePath = Path.GetFullPath(Path.Combine(@"\data_samples", "nano_img.png"));
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
            var result = client.UploadFileAsync(imagePath, "AC-1").GetAwaiter().GetResult();

            var waitReponse = client.WaitForMediaAsync(result.Mid, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(3)).GetAwaiter().GetResult();
            Console.WriteLine(waitReponse.Mid);
        }
    }
}
