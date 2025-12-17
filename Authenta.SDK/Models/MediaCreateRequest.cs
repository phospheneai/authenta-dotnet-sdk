
namespace Authenta.SDK.Models
{
    public class MediaCreateRequest
    {
        public string name { get; set; }
        public string contentType { get; set; }
        public long size { get; set; }
        public string modelType { get; set; }
    }

}