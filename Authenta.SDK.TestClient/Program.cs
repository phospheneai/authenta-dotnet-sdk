using System;
using System.Threading.Tasks;
using Authenta.SDK;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Authenta SDK Test Client");

        var baseDir = AppContext.BaseDirectory;
        var imagePath = Path.GetFullPath(Path.Combine(baseDir,  "../../../..", "data_samples", "nano_img.png"));
        Console.WriteLine("source data path: " + imagePath);
        var options = new AuthentaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
            ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET")
        };
        
        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret)){
            throw new InvalidOperationException("Authenta credentials are not configured.");
        }
        var client = new AuthentaClient(options);
         var result1 = await client.UploadProcessAndWaitAsync(ex1, "DF-1", TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(3));


        Console.WriteLine($"Media ID: {result.Mid}");
    }
}
