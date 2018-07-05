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
                await fs.ReadAsync(result, 0, (int)fi.Length);
                fs.Close();
            }

            return result;
        }

        public static async Task<string> ReadText(string fileName)
        {
            var fileEncoding = GetEncoding(fileName);
            var bytes = await Read(fileName);
            var utf8Bytes = Encoding.Convert(fileEncoding, Encoding.UTF8, bytes);
            return Encoding.UTF8.GetString(utf8Bytes);
        }

        private static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
            return Encoding.GetEncoding(1251);
        }
    }
}
