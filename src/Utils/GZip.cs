using System.IO;
using System.IO.Compression;

namespace Cherry.Net.Utils
{
    public static class GZip
    {
        private const int BufferSize = 8096;

        public static byte[] Compress(byte[] data, int index, int len)
        {
            using (var source = new MemoryStream(data, index, len))
            {
                using (var dest = new MemoryStream())
                {
                    Compress(source, dest);
                    return dest.ToArray();
                }
            }
        }

        public static void Compress(Stream source, Stream dest)
        {
            using (var gs = new GZipStream(dest, CompressionMode.Compress, true))
            {
                var bs = new byte[BufferSize];
                int i;
                while ((i = source.Read(bs, 0, bs.Length)) > 0) gs.Write(bs, 0, i);
                gs.Flush();
            }
        }

        public static void Decompress(Stream source, Stream dest)
        {
            using (var gs = new GZipStream(source, CompressionMode.Decompress, true))
            {
                var bs = new byte[BufferSize];
                int i;
                while ((i = gs.Read(bs, 0, bs.Length)) > 0) dest.Write(bs, 0, i);
            }
        }

        public static byte[] Decompress(byte[] data, int index, int len)
        {
            using (var source = new MemoryStream(data, index, len))
            {
                using (var dest = new MemoryStream())
                {
                    Decompress(source, dest);
                    return dest.ToArray();
                }
            }
        }
    }
}