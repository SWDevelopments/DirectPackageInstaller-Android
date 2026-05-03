using System.IO;

namespace DirectPackageInstaller
{
    internal static class TransferTuning
    {
        public const int HttpServerReadTimeoutMs = 1000 * 60 * 10;
        public const int HttpServerBufferSize = 1024 * 1024 * 2;
        public const int NetworkCacheSize = 1024 * 1024;
        public const int NetworkBufferSize = 1024 * 1024;
        public const int DiskBufferSize = 1024 * 1024;
        public const int NetworkRetryCount = 5;
        public const int UpstreamConnectTimeoutMs = 1000 * 30;
        public const int UpstreamReadWriteTimeoutMs = 1000 * 60 * 5;

        public const FileOptions TempFileOptions = FileOptions.RandomAccess;
        public const FileOptions LocalPackageFileOptions = FileOptions.SequentialScan;
    }
}
