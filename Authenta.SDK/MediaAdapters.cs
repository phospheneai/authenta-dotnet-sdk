using Authenta.SDK.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace Authenta.SDK
{
    public static class MediaAdapters
    {
        public static IDictionary<string, object> ToVisualizationDict(MediaStatusResponse media)
        {
            var dict = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(media.Result))
                dict["resultURL"] = media.Result;

            bool isImageModel = media.ModelType?.StartsWith("AC-", StringComparison.OrdinalIgnoreCase) ?? false;

            if (isImageModel && !string.IsNullOrEmpty(media.Heatmap))
            {
                dict["type"] = "image";
                dict["heatmapURL"] = media.Heatmap;
            }
            else if (media.Participants != null && media.Participants.Count > 0)
            {
                dict["type"] = "video";
                var participantsArray = new JArray();
                foreach (var p in media.Participants)
                {
                    var pObj = new JObject();
                    if (!string.IsNullOrEmpty(p.Heatmap))
                        pObj["heatmap"] = p.Heatmap;
                    participantsArray.Add(pObj);
                }
                dict["participants"] = participantsArray;
            }

            return dict;
        }
    }
}