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
            var bytes = await Read(fileName);
            var utf8Bytes = Encoding.Convert(fileEncoding, Encoding.UTF8, bytes);
            return NormalizeUtf8(fileEncoding, Encoding.UTF8.GetString(utf8Bytes));
        }

        private static string NormalizeUtf8(Encoding encoding, string text)
        {
            return !Equals(Encoding.GetEncoding(1251), encoding) ? text.Remove(0, 1) : text;
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