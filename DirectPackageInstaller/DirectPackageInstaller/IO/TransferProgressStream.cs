using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DirectPackageInstaller.IO
{
    internal sealed class TransferProgressStream : Stream
    {
        private readonly Stream BaseStream;
        private readonly Action<int> Progress;

        public TransferProgressStream(Stream baseStream, Action<int> progress)
        {
            BaseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => BaseStream.Length;

        public override long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        public override void Flush() => BaseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = BaseStream.Read(buffer, offset, count);
            if (read > 0)
                Progress(read);
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var read = await BaseStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (read > 0)
                Progress(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await BaseStream.ReadAsync(buffer, cancellationToken);
            if (read > 0)
                Progress(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
        public override void SetLength(long value) => BaseStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                BaseStream.Dispose();

            base.Dispose(disposing);
        }
    }
}
