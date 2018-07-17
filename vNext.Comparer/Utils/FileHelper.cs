using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace vNext.Comparer.Utils
{
    public static class FileHelper
    {
        public static async Task<byte[]> Read(string filename)
        {
            var fi = new FileInfo(filename);

            byte[] result;
            using (var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                result = new byte[fi.Length];
                await fs.ReadAsync(result, 0, (int) fi.Length);
                fs.Close();
            }

            return result;
        }

        public static async Task<string> ReadText(string fileName)
        {
            var fileEncoding = GetFileEncoding(fileName);
            var bytes = await Read(fileName).ConfigureAwait(false);
            var utf8Bytes = Encoding.Convert(fileEncoding, Encoding.UTF8, bytes);
            return NormalizeUtf8(fileEncoding, utf8Bytes);
        }

        private static string NormalizeUtf8(Encoding encoding, byte[] bytes)
        {
            if (Equals(Encoding.Unicode, encoding))
            {
                return Encoding.UTF8.GetString(bytes).Remove(0, 1);
            }
            if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes).Remove(0, 1);
            }

            return Encoding.UTF8.GetString(bytes);
        }

        private static Encoding GetFileEncoding(string fileName)
        {
            using (var fs = File.OpenRead(fileName))
            {
                var cdet = new Ude.CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();

                if (cdet.Charset == null)
                {
                    throw new ArgumentException("Error in reading charset.");
                }

                return Encoding.GetEncoding(cdet.Charset);
            }
        }
    }
}