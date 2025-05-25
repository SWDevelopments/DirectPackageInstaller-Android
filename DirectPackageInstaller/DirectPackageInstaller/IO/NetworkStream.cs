using DirectPackageInstaller.FileHosts;
using NFSLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Web;
using NFSVersion = NFSLibrary.NFSClient.NFSVersion;

namespace DirectPackageInstaller.IO
{
    public class NetworkStream : Stream, IDisposable
    {
        public WebProxy Proxy = null;

        public Action RefreshUrl = null;
        public string FinalURL { get; private set; }
        private string FinalContentType;

        public List<(string Key, string Value)> Headers = new List<(string Key, string Value)>();
        public CookieContainer Cookies = new CookieContainer();
        string _fn;
        public string? Filename
        {
            get {
                if (Length == 0)
                    return null;
                return _fn;
            }
        }

        public int Timeout { get; set; }
        public bool KeepAlive { get; set; } = false;

        private const int CacheLen = 1024 * 8;

        // Cache for short requests.
        private byte[] cache;
        private int cacheLen;
        //private WebResponse response;
        private long? length;
        private long cachePosition;
        private int cacheCount;

        private bool NFSMode = false;
        private NFSClient nfsClient = null;
        public NetworkStream(string url, int cacheLen = CacheLen)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("url empty");
            if (cacheLen <= 0)
                throw new ArgumentException("cacheLen must be greater than 0");

            if (url.StartsWith("nfs:", StringComparison.InvariantCultureIgnoreCase))
            {
                NFSMode = true;
                NetRead = NfsReader;
            }
            else
            {
                NetRead = HttpReader;
            }

            Url = url;
            this.cacheLen = cacheLen;
            cache = new byte[cacheLen];
        }

        ~NetworkStream()
        {
            Dispose();
        }

        public string Url { get; protected set; }

        private string NfsPathOverride = null;
        private string NfsPath => NfsPathOverride ?? new Uri(Url).AbsolutePath;

        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanSeek => length == null || length > 0;

        public override long Position { get; set; }

        /// <summary>
        /// Lazy initialized length of the resource.
        /// </summary>
        public override long Length
        {
            get
            {
                if (length == null)
                    length = NFSMode ? NFSGetLength() : HttpGetLength();
                return length ?? -1;
            }
        }

        /// <summary>
        /// Count of HTTP requests. Just for statistics reasons.
        /// </summary>
        public int HttpRequestsCount { get; private set; }

        /// <summary>
        /// Close the Http Connection but keep the stream
        /// available for reuse.
        /// </summary>
        public void CloseConnection()
        {
            if (nfsClient != null)
            {
                if (nfsClient.IsMounted)
                    nfsClient.UnMountDevice();
                if (nfsClient.IsConnected)
                    nfsClient.Disconnect();
                nfsClient = null;
            }

            if (ResponseStream != null)
            {
                ResponseStream?.Close();
                ResponseStream?.Dispose();
                ResponseStream = null;
            }

            if (req != null){
                req.ServicePoint.CloseConnectionGroup(req.ConnectionGroupName);
                req = null;
            }

            if (resp != null)
            {
                resp?.Close();
                resp?.Dispose();
                resp = null;
            }
        }
        public override void SetLength(long value)
        { throw new NotImplementedException(); }

        private SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;

            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset >= buffer.Length)
                throw new ArgumentException(nameof(offset));
            if (count < 0 || offset + count > buffer.Length)
                throw new ArgumentException(nameof(count));

#if DEBUG
            if (Semaphore.CurrentCount == 0)
                System.Diagnostics.Debugger.Break();
#endif

            Semaphore.Wait();

            try 
            {
                long curPosition = Position;
                Position += ReadFromCache(buffer, ref offset, ref count);

                if (count > cacheLen)
                {
                    int EmptyTries = 3;
                    // large request, do not cache
                    while (count > 0)
                    {
                        int Readed;
                        Position += Readed = NetRead(buffer, ref offset, ref count);

                        if (Readed == 0 && EmptyTries-- < 0)
                            break;
                        else if (Readed > 0)
                            EmptyTries = 3;
                    }
                }
                else if (count > 0)
                {
                    // read to cache
                    cachePosition = Position;
                    int off = 0;
                    int len = cacheLen;
                    cacheCount = NetRead(cache, ref off, ref len);
                    Position += ReadFromCache(buffer, ref offset, ref count);
                }

                int TotalReaded = (int)(Position - curPosition);
                return TotalReaded;
            }
            finally
            {
                Semaphore.Release();
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        { throw new NotImplementedException(); }

        public override long Seek(long pos, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.End:
                    Position = Length + pos;
                    break;

                case SeekOrigin.Begin:
                    Position = pos;
                    break;

                case SeekOrigin.Current:
                    Position += pos;
                    break;
            }
            return Position;
        }

        public override void Flush()
        {
        }

        private int ReadFromCache(byte[] buffer, ref int offset, ref int count)
        {
            if (cachePosition > Position || (cachePosition + cacheCount) <= Position)
                return 0; // cache miss
            int ccOffset = (int)(Position - cachePosition);
            int ccCount = Math.Min(cacheCount - ccOffset, count);
            Array.Copy(cache, ccOffset, buffer, offset, ccCount);
            offset += ccCount;
            count -= ccCount;
            return ccCount;
        }

        HttpWebRequest req = null;
        WebResponse resp = null;
        Stream ResponseStream = null;
        long RespPos = 0;

        private delegate int ReadHandler(byte[] buffer, ref int offset, ref int count, int Tries = 0);

        private ReadHandler NetRead;

        private int NfsReader(byte[] buffer, ref int offset, ref int count, int Tries = 0)
        {
            try
            {
                if (Position == Length)
                    return 0;

                if (!nfsClient.IsConnected || !nfsClient.IsMounted) {
                    nfsClient = getNFSConnection();
                }

                int nread = 0;

                byte[] target = null;
               
                nread = (int)nfsClient.Read(NfsPath, Position, Position + count, ref target);

                nread = Math.Min(nread, count);

                if (buffer.Length < nread + offset || target.Length < nread)
                    System.Diagnostics.Debugger.Break();

                Array.Copy(target, 0, buffer, offset, nread);

                offset += nread;
                count -= nread;

                return nread;
            }
            catch
            {
                ResponseStream?.Dispose();
                ResponseStream = null;

                if (Tries < 3)
                {
                    RefreshUrl?.Invoke();
                    return NetRead(buffer, ref offset, ref count, Tries + 1);
                }

                throw;
            }
        }

        private int HttpReader(byte[] buffer, ref int offset, ref int count, int Tries = 0)
        {
            try
            {
                if (Position == Length)
                    return 0;
                
                if (RespPos != Position || ResponseStream == null)
                {
                    if (Url == null)
                        RefreshUrl?.Invoke();
                    
                    HttpRequestsCount++;

                    if (ResponseStream != null)
                    {
                        ResponseStream?.Close();
                        ResponseStream?.Dispose();
                    }


                    if (req != null)
                        req.ServicePoint.CloseConnectionGroup(req.ConnectionGroupName);


                    if (resp != null)
                    {
                        resp?.Close();
                        resp?.Dispose();
                    }

                    req = WebRequest.CreateHttp(Url);
                    req.ConnectionGroupName = Guid.NewGuid().ToString();
                    req.CookieContainer = Cookies;
                    req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    req.UserAgent = FileHostBase.UserAgent;


                    req.KeepAlive = KeepAlive;
                    req.ServicePoint.SetTcpKeepAlive(KeepAlive, 1000 * 60 * 5, 1000);

                    req.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(SllBypass);

                    foreach (var Header in Headers)
                        req.Headers[Header.Key] = Header.Value;

                    if (length > 0 || Position > 0)
                        req.AddRange(Position, Length - 1);
                    
                    resp = req.GetResponse();

                    if (length != null && FinalURL != resp.ResponseUri.AbsoluteUri)
                    {
                        if (resp.ContentType.Contains("text/html") && resp.ContentType != FinalContentType)
                        {
                            FinalURL = resp.ResponseUri.AbsoluteUri;
                            FinalContentType = resp.ContentType;
                            throw new WebException("Link Expired?");
                        }
                    }

                    FinalURL = resp.ResponseUri.AbsoluteUri;
                    FinalContentType = resp.ContentType;

                    if (length == null)
                        ReadResponseInfo(resp);

                    ResponseStream = resp.GetResponseStream();
                    ResponseStream = new BufferedStream(ResponseStream);
                }

                int nread = 0;

                int Readed = 0;

                
                do
                {
                    Readed = ResponseStream.Read(buffer, offset + nread, count - nread);
                    nread += Readed;
                } while (Readed > 0 && count > 0);

                if (Readed == 0 && length is null or -1)
                    length = nread;

                offset += nread;
                count -= nread;
                
                if (App.IsUnix && nread == 0 && count > 0 && ResponseStream.Position != ResponseStream.Length)
                    throw new WebException();

                RespPos = Position + nread;

                return nread;

            }
            catch (IOException ex)
            {
                ResponseStream?.Dispose();
                ResponseStream = null;

                if (Tries < 3)
                {
                    Thread.Sleep(Tries * 500);

                    RefreshUrl?.Invoke();
                    return NetRead(buffer, ref offset, ref count, Tries + 1);
                }

                throw;
            }
            catch (Exception ex)
            {
                if (req != null)
                    req.ServicePoint.CloseConnectionGroup(req.ConnectionGroupName);

                ResponseStream?.Dispose();
                ResponseStream = null;

                if (Tries < 3)
                {
                    RefreshUrl?.Invoke();
                    return NetRead(buffer, ref offset, ref count, Tries + 1);
                }
                
                if (ex is WebException)
                {
                    var response = (HttpWebResponse)((WebException)ex).Response;
                    if (response?.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable)
                        return 0;
                }

                throw;
            }
        }

        private bool SllBypass(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        static Dictionary<string, (string Filename, long Length)> HeadCache = new Dictionary<string, (string Filename, long Length)>();

        private IPAddress getNFSIp()
        {
            var Host = new Uri(Url).Host;
            if (IPAddress.TryParse(Host, out IPAddress? Address))
                return Address;

            return Dns.GetHostEntry(Host).AddressList.First();
        }

        private NFSClient getNFSConnection()
        {
            if (nfsClient != null && nfsClient.IsConnected)
                return nfsClient;

            NFSClient? client = null;

            Exception lastError = null;

            foreach (var version in new[] { NFSVersion.v4, NFSVersion.v3, NFSVersion.v2 })
            {
                try
                {
                    client = new NFSClient(version);
                    client.Connect(getNFSIp());
                    if (client.IsConnected)
                        break;
                }
                catch (Exception ex){ 
                    lastError = ex;
                }
            }

            if (client == null || !client.IsConnected)
                throw new IOException("NFS connection failed\n" + lastError?.Message);

            var mounts = client.GetExportedDevices().ToArray();

            if (mounts.Length >= 0)
            {
                //subPath is used to find the file in paths that include the mount point
                string subPath = null;
                string mountPoint = null;

                if (NfsPath.Trim('/').Contains("/"))
                {
                    subPath = NfsPath.Trim('/');
                    mountPoint = subPath.Substring(0, subPath.IndexOf("/"));
                    subPath = subPath.Substring(subPath.IndexOf("/"));

                    var mountQuery = mounts.Where(x => x.Trim('/').Equals(mountPoint));

                    if (mountQuery.Any())
                    {
                        mounts = mountQuery.ToArray();
                    }
                }

                foreach (var mount in mounts)
                {
                    try
                    {
                        if (client.IsMounted)
                            client.UnMountDevice();

                        client.MountDevice(mount);

                        if (client.FileExists(NfsPath))
                        {
                            return client;
                        }

                        if (subPath != null && client.FileExists(subPath))
                        {
                            NfsPathOverride = subPath;
                            return client;
                        }
                    }
                    catch { }

                }
            }
            else
            {
                client.MountDevice(mounts.First());
            }


            return client;
        }

        private long? NFSGetLength()
        {
            if (Url == null)
                RefreshUrl?.Invoke();

            nfsClient = getNFSConnection();

            var attribs = nfsClient.GetItemAttributes(NfsPath, true);

            return attribs.Size;
        }

        private long? HttpGetLength(bool NoHead = false)
        {
            if (Url == null)
                RefreshUrl?.Invoke();
            
            if (HeadCache.ContainsKey(Url))
            {
                _fn = HeadCache[Url].Filename;
                return HeadCache[Url].Length;
            }

            HttpRequestsCount++;
            try
            {
                HttpWebRequest request = WebRequest.CreateHttp(Url);
                request.UserAgent = FileHostBase.UserAgent;
                request.ConnectionGroupName = Guid.NewGuid().ToString();
                request.KeepAlive = false;
                request.CookieContainer = Cookies;
                request.Proxy = Proxy;
                request.Method = NoHead ? "GET" : "HEAD";
                request.Timeout = 15000;

                request.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(SllBypass);


                foreach (var Header in Headers)
                    request.Headers[Header.Key] = Header.Value;

                using var response = request.GetResponse();
                ReadResponseInfo(response);
                response?.Close();

                request.ServicePoint.CloseConnectionGroup(request.ConnectionGroupName);
            }
            catch
            {
                if (NoHead)
                    return null;
                
                return HttpGetLength(true);
            }

            return Length;
        }

        void ReadResponseInfo(WebResponse Response)
        {
            FinalURL = Response.ResponseUri.AbsoluteUri;
            FinalContentType = Response.ContentType;

            if (Response.Headers.AllKeys.Contains("Content-Disposition"))
            {
                var Disposition = Response.Headers["Content-Disposition"]!;
                const string prefix = "filename=";
                if (Disposition.Contains(prefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    Disposition = Disposition.Substring(Disposition.IndexOf(prefix, StringComparison.InvariantCultureIgnoreCase) + prefix.Length).Trim('"');
                    _fn = HttpUtility.UrlDecode(Disposition.Split(';').First().Trim('"'));
                }
            }

            var Length = Response.ContentLength;

            length = Length;

            HeadCache[Url] = (_fn, Length);
        }

        private new void Dispose()
        {
            CloseConnection();
            base.Dispose();
        }
    }
}
