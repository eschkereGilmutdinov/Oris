using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniHttpServer_3
{
    internal class ContentTypes
    {
        private static readonly Dictionary<string, string> TypesExtesionsDict = new Dictionary<string, string>
        {
            { "html", "text/html" },
            { "css", "text/css" },
            { "js", "application/javascript" },
            { "json", "application/json" },
            { "png", "image/png" },
            { "jpeg", "image/jpeg" },
            { "jpg", "image/jpeg" },
            { "webp", "image/webp" },
            { "svg", "image/svg+xml" },
            { "ico", "image/x-icon" },
        };

        public static string GetContentType(string extension)
        {
            if (TypesExtesionsDict.ContainsKey(extension))
                return TypesExtesionsDict[extension];

            return "";
        }

        public static bool IsTextContent(string contentType)
        {
            return contentType.StartsWith("text/") ||
                   contentType == "application/javascript" ||
                   contentType == "application/json";
        }
    }
}
