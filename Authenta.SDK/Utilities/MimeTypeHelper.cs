using System;
using System.IO;
using Authenta.SDK.Exceptions;
namespace Authenta.SDK
{
    public static class MimeTypeHelper
    {
        public static string GetMimeType(string filePath){
            string ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            switch (ext){
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".mp4":
                    return "video/mp4";
                default:
                    throw new AuthentaException(
                        $"Unsupported file type: {ext}");
            }
        }
    }
}
