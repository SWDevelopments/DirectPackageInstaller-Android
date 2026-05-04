using System;

namespace DirectPackageInstaller.Host
{
    public sealed class TransferProgressInfo
    {
        public TransferProgressInfo(
            string requestPath,
            long bytesSent,
            long totalBytes,
            long responseBytesSent,
            long responseBytesTotal,
            DateTime startedAt,
            DateTime updatedAt,
            bool completed)
        {
            RequestPath = requestPath;
            BytesSent = Math.Max(0, Math.Min(bytesSent, totalBytes));
            TotalBytes = Math.Max(0, totalBytes);
            ResponseBytesSent = Math.Max(0, Math.Min(responseBytesSent, responseBytesTotal));
            ResponseBytesTotal = Math.Max(0, responseBytesTotal);
            StartedAt = startedAt;
            UpdatedAt = updatedAt;
            Completed = completed;
        }

        public string RequestPath { get; }
        public long BytesSent { get; }
        public long TotalBytes { get; }
        public long ResponseBytesSent { get; }
        public long ResponseBytesTotal { get; }
        public DateTime StartedAt { get; }
        public DateTime UpdatedAt { get; }
        public bool Completed { get; }

        public double Percent => TotalBytes <= 0 ? 0 : (double)BytesSent / TotalBytes;

        public double BytesPerSecond
        {
            get
            {
                var seconds = (UpdatedAt - StartedAt).TotalSeconds;
                return seconds <= 0 ? 0 : ResponseBytesSent / seconds;
            }
        }

        public static string FormatBytes(double bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            var value = bytes;
            var unit = 0;

            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return $"{value:0.##} {units[unit]}";
        }
    }
}
