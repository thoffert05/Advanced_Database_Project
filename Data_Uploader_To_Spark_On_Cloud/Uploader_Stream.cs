using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/// <summary>
/// this is a file stream which returns progress of the file read
/// it will be used to give the progress of the file being uploaded to the cloud
namespace Data_Uploader_To_Spark_On_Cloud
{
    internal class Uploader_Stream:Stream
    {
        private readonly Stream base_stream;
        private readonly IProgress<double> progress_interface;
        private long bytesTransferred = 0;
        private readonly long totalBytes;

        public Uploader_Stream(Stream inner, long totalBytes, IProgress<double> progress)
        {
            base_stream = inner;
            this.totalBytes = totalBytes;
            progress_interface = progress;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base_stream.Write(buffer, offset, count);
            bytesTransferred += count;
            //compute the progress percentage and report it
            double pct = (double)bytesTransferred / totalBytes;
            progress_interface.Report(pct);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await base_stream.WriteAsync(buffer, offset, count, cancellationToken);
            bytesTransferred += count;
            //compute the progress percentage and report it
            double pct = (double)bytesTransferred / totalBytes;
            progress_interface.Report(pct);
        }

        // Required overrides
        public override bool CanRead => base_stream.CanRead;
        public override bool CanSeek => base_stream.CanSeek;
        public override bool CanWrite => base_stream.CanWrite;
        public override long Length => base_stream.Length;
        public override long Position { get => base_stream.Position; set => base_stream.Position = value; }
        public override void Flush() => base_stream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => base_stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => base_stream.Seek(offset, origin);
        public override void SetLength(long value) => base_stream.SetLength(value);

    }
}
