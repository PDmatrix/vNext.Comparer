using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace vNext.Comparer.Utils
{
    public static class FileHelper
    {
        /// <summary>
        /// Returns the array of bytes from the file
        /// </summary>
        /// <param name="filename">File to read</param>
        /// <returns></returns>
        public static async Task<byte[]> ReadAsync(string filename)
        {
            var fi = new FileInfo(filename);

            byte[] result;
            using (var fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                result = new byte[fi.Length];
                await fs.ReadAsync(result, 0, (int) fi.Length).ConfigureAwait(false);
                fs.Close();
            }

            return result;
        }
        /// <summary>
        /// Returns the converted in UTF8 text of the file
        /// </summary>
        /// <param name="fileName">File to read</param>
        /// <returns></returns>
        public static async Task<string> ReadTextUtf8Async(string fileName)
        {
            var fileEncoding = GetFileEncoding(fileName);
            var bytes = await ReadAsync(fileName).ConfigureAwait(false);
            var utf8Bytes = Encoding.Convert(fileEncoding, Encoding.UTF8, bytes);
            return NormalizeBytesUtf8(fileEncoding, utf8Bytes);
        }
        /// <summary>
        /// Returns the normalized string
        /// </summary>
        /// <param name="encoding">Encoding of the bytes</param>
        /// <param name="bytes">Bytes to normalize</param>
        /// <returns></returns>
        private static string NormalizeBytesUtf8(Encoding encoding, byte[] bytes)
        {
            if (Equals(Encoding.Unicode, encoding))
            {
                return Encoding.UTF8.GetString(bytes).Remove(0, 1);
            }
            // Remove UTF8 BOM
            if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes).Remove(0, 1);
            }

            return Encoding.UTF8.GetString(bytes);
        }
        /// <summary>
        /// Returns encoding of the file
        /// </summary>
        /// <param name="fileName">File to get encoding from</param>
        /// <returns></returns>
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
        /// <summary>
        /// Creates the directory. If the directory exists, then it and all the files in it are deleted.
        /// </summary>
        /// <param name="path">Path to create directory</param>
        public static void CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }
        /// <summary>
        /// Writes text to a file with Encoding UTF8.
        /// </summary>
        /// <param name="path">File to write</param>
        /// <param name="text">Text to write in file</param>
        public static void WriteInFile(string path, string text)
        {
            File.WriteAllText(path, text, Encoding.UTF8);
        }
        /// <summary>
        /// Writes text to a file with specified encoding.
        /// </summary>
        /// <param name="path">File to write</param>
        /// <param name="text">Text to write in file</param>
        /// <param name="encoding">Encoding to use</param>
        public static void WriteInFile(string path, string text, Encoding encoding)
        {
            File.WriteAllText(path, text, encoding);
        }
    }
}