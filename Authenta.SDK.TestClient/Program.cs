using System;
using System.Threading.Tasks;
using Authenta.SDK;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Authenta SDK Test Client");

        var ex1 = @"C:\DG-client-project\authenta-python-sdk\data_samples\nano_img.png";


        var client = new __AuthentaClient(new AuthentaOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("AUTHENTA_BASE_URL"),
            ClientId = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_ID"),
            ClientSecret = Environment.GetEnvironmentVariable("AUTHENTA_CLIENT_SECRET")
        });

        if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret)){
            throw new InvalidOperationException("Authenta credentials are not configured.");
        }

        var result = await client.UploadFileAsync(ex1, "AC-1");

        Console.WriteLine($"Media ID: {result.Mid}");
    }
}
