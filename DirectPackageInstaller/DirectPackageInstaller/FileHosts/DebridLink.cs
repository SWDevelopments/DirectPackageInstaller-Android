using DirectPackageInstaller.Others;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;

namespace DirectPackageInstaller.FileHosts
{
    class DebridLink : FileHostBase
    {
        static DebridLinkRoot? HostsRegex = null;
        static Dictionary<string, string> GenCache = new Dictionary<string, string>();

        public override string HostName => "DebridLink";
        public override bool Limited => false;

        public override DownloadInfo GetDownloadInfo(string URL)
        {
            if (GenCache.ContainsKey(URL))
                return new DownloadInfo() { Url = GenCache[URL] };

            const string URLMask = "https://debrid-link.com/api/v2/downloader/add?access_token={0}";

            var PostData = JsonSerializer.Serialize(new UrlEntry(URL), JSONContext.Default.Options);

            var Response = PostString(string.Format(URLMask, App.Config.DebridLinkApiKey), "application/json", PostData);

            if (string.IsNullOrWhiteSpace(Response))
                throw new Exception();
            
            var Data = JsonSerializer.Deserialize<DebridLinkApi>(Response, JSONContext.Default.Options);

            if (!Data.success)
                throw new Exception("DebridLink Api Failed: " + Data.error);

            GenCache[URL] = Data.value.downloadUrl;

            return new DownloadInfo()
            {
                Url = GenCache[URL] = Data.value.downloadUrl
            };
        }

        public override bool IsValidUrl(string URL)
        {
            if (!App.Config.UseDebridLink || App.Config.DebridLinkApiKey.ToLowerInvariant() == "null" || string.IsNullOrEmpty(App.Config.DebridLinkApiKey))
                return false;

            while (!HostsRegex.HasValue || !HostsRegex.Value.success)
            {
                var Status = DownloadString("https://debrid-link.com/api/v2/downloader/regex?access_token=" + App.Config.DebridLinkApiKey);
                HostsRegex = JsonSerializer.Deserialize<DebridLinkRoot>(Status, JSONContext.Default.Options);
            }

            foreach (var Host in HostsRegex.Value.value.SelectMany(x => x.regexs)) {
                try
                {
                    if (new Regex(Host, RegexOptions.None).IsMatch(URL))
                        return true;
                }
                catch { }

                try
                {
                    if (new Regex(Host.Trim('/'), RegexOptions.None).IsMatch(URL))
                        return true;
                }
                catch { }
            }

            return false;
        }
    }


}
