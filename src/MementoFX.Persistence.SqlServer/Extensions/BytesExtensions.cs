using System.IO;
using System.IO.Compression;

namespace System
{
    public static class BytesExtensions
    {
        public static byte[] GZipCompress(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }

                return memoryStream.ToArray();
            }
        }

        public static byte[] GZipDecompress(this byte[] gzipBytes)
        {
            if (gzipBytes == null || gzipBytes.Length == 0)
            {
                return null;
            }

            var gzipBuffer = new byte[4];

            Array.Copy(gzipBytes, gzipBytes.Length - 4, gzipBuffer, 0, 4);

            var bytesLength = BitConverter.ToInt32(gzipBuffer, 0);

            var bytes = new byte[bytesLength];

            using (var memoryStream = new MemoryStream(gzipBytes))
            {
                using (var gzip = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gzip.Read(bytes, 0, bytesLength);
                }
            }

            return bytes;
        }
    }
}
