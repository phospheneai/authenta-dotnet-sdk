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
            BaseUrl = "https://dev.console.authenta.ai/",
            ClientId = "6940fe0d723ef480cf78f546",
            ClientSecret = "PywvtYiB7BGe1MCF8NVtBkj3Sd49AfDc",
        });

        var result = await client.UploadFileAsync(ex1, "AC-1");

        Console.WriteLine($"Media ID: {result.Mid}");
    }
}
