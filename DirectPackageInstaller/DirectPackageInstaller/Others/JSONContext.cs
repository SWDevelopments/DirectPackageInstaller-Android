using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DirectPackageInstaller.Others
{
    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(PKGManifest))]
    [JsonSerializable(typeof(AllDebridApi))]
    [JsonSerializable(typeof(RealdebirdApi))]
    public partial class JSONContext : JsonSerializerContext
    {
    }

    public struct PKGManifest
    {
        public long originalFileSize { get; set; }
        public string packageDigest { get; set; }
        public int numberOfSplitFiles { get; set; }
        public PkgPiece[] pieces { get; set; }
    }

    public struct PkgPiece
    {
        public string url { get; set; }
        public long fileOffset { get; set; }
        public long fileSize { get; set; }
        public string hashValue { get; set; }
    }

    public struct AllDebridApi
    {
        public string status { get; set; }
        public AllDebridApiData data { get; set; }
    }

    public struct AllDebridApiData
    {
        public Dictionary<string, AllDebridHostsEntry> hosts { get; set; }

        public string link { get; set; }
        public string host { get; set; }
        public string hostDomain { get; set; }
        public string filename { get; set; }
        public bool paws { get; set; }
        public long filesize { get; set; }
        public string id { get; set; }
    }

    public struct AllDebridHostsEntry
    {
        public string name { get; set; }
        public string type { get; set; }
        public string[] domains { get; set; }
        public string[] regexps { get; set; }
        public object regexp { get; set; }
        public bool status { get; set; }
    }

    public struct RealdebirdApi
    {
        public string id { get; set; }
        public string filename { get; set; }
        public string mimeType { get; set; }
        public long filesize { get; set; }
        public string link { get; set; }
        public string host { get; set; }
        public long chunks { get; set; }
        public int crc { get; set; }
        public string download { get; set; }
        public int streamable { get; set; }
    }
}
