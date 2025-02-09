using DirectPackageInstaller.Others;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace DirectPackageInstaller.FileHosts
{
    class AllDebrid : FileHostBase
    {

        static AllDebridApi? Info = null;
        static Dictionary<string, string> GenCache = new Dictionary<string, string>();

        public override string HostName => "AllDebrid";
        public override bool Limited => false;

        public override DownloadInfo GetDownloadInfo(string URL)
        {
            if (GenCache.ContainsKey(URL))
                return new DownloadInfo() { Url = GenCache[URL] };

            const string URLMask = "https://api.alldebrid.com/v4/link/unlock?agent=DirectPackageInstaller&apikey={0}&link={1}";

            var Response = DownloadString(string.Format(URLMask, App.Config.AllDebridApiKey, HttpUtility.UrlEncode(URL)));
            var Data = JsonSerializer.Deserialize<AllDebridApi>(Response, JSONContext.Default.Options);

            if (Info?.status != "success")
                throw new Exception();

            GenCache[URL] = Data.data.link;

            return new DownloadInfo()
            {
                Url = GenCache[URL] = Data.data.link
            };
        }

        public override bool IsValidUrl(string URL)
        {
            if (!App.Config.UseAllDebrid || App.Config.AllDebridApiKey.ToLowerInvariant() == "null" || string.IsNullOrEmpty(App.Config.AllDebridApiKey))
                return false;

            if (Info == null)
            {
                var Status = DownloadString("https://api.alldebrid.com/v4/user/hosts?agent=DirectPackageInstaller&apikey=" + App.Config.AllDebridApiKey);
                Info = JsonSerializer.Deserialize<AllDebridApi>(Status, JSONContext.Default.Options);
            }

            if (Info?.status != "success")
                return false;

            foreach (var Host in Info?.data.hosts) {
                foreach (var exp in Host.Value.regexps)
                {
                    try
                    {
                        if (new Regex(exp).IsMatch(URL))
                            return true;
                    }
                    catch { }
                }
            }

            return false;
        }
    }


}
