using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace XtbCnb2
{
    public class Log
    {
        private static string LOG_DIR = GetUserDataPath();

        /// <summary>
        /// Logs a string entry with date and time prepended.
        /// Does what you'd expect from a standard logger function.
        /// Uses daily log files.
        /// </summary>
        /// <param name="text"></param>
        public static void LogAsText(string text)
        {
            DateTime now = DateTime.UtcNow;
            File.AppendAllText(LOG_DIR + CurrFilename() + ".log", string.Format("{0:0000}{1:00}{2:00} {3:00}{4:00}{5:00}: {6}\n", now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, text), Encoding.UTF8);
        }

        // this is what an empty zip file looks like. cool, huh?
        private static byte[] EMPTY_ZIP = { 0x50, 0x4b, 0x05, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        /// <summary>
        /// Saves a (looong) string of data as a daily zip file entry.
        /// </summary>
        /// <param name="fileContents">The data as string. NOT filename, sorry</param>
        /// <param name="filenameInZip">What the file will be called in the zip file</param>
        public static void LogAsFile(string fileContents, string filenameInZip)
        {
            string filename = LOG_DIR + CurrFilename() + ".zip";
            if (!File.Exists(filename))
            {
                using (FileStream newzip = File.OpenWrite(filename))
                {
                    newzip.Write(EMPTY_ZIP, 0, EMPTY_ZIP.Length);
                }
            }

            using (ZipFile zipfile = new ZipFile(filename))
            {

                MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(fileContents));
                LogDataSource lds = new LogDataSource(ms);

                zipfile.BeginUpdate();
                zipfile.Add(lds, filenameInZip, CompressionMethod.Deflated, false);
                zipfile.CommitUpdate();
            }
        }

        /// <summary>
        /// Creates and returns a data directory for this app.
        /// </summary>
        /// <returns>Path to the directory. Always ends with a backslash.</returns>
        private static string GetUserDataPath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dir = System.IO.Path.Combine(dir, "XtbCnb");
            
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!dir.EndsWith("\\"))
                dir += "\\";

            return dir;
        }

        /// <summary>
        /// Return current (daily) filename part of the log file.
        /// Prepend path, append extension and voila - you got the log file.
        /// </summary>
        /// <returns></returns>
        public static string CurrFilename()
        {
            DateTime now = DateTime.UtcNow;

            return string.Format("{0:0000}-{1:00}-{2:00}", now.Year, now.Month, now.Day);
        }
    }

    /// <summary>
    /// This is for the SharpZipLib's sake
    /// </summary>
    public class LogDataSource : IStaticDataSource
    {
        private Stream _stream;

        /// <summary>
        /// Construct with the stream the this thing will be returning
        /// </summary>
        /// <param name="stream"></param>
        public LogDataSource(Stream stream)
        {
            SetStream(stream);
        }

        /// <summary>
        /// Implementation of the method from IStaticDataSource
        /// </summary>
        /// <returns></returns>
        public Stream GetSource()
        {
            return _stream;
        }

        /// <summary>
        /// Call this to set the stream to something useful.
        /// Or just use the constructor, what the hell...
        /// </summary>
        /// <param name="inputStream"></param>
        public void SetStream(Stream inputStream)
        {
            _stream = inputStream;
//            _stream.Position = 0;
        }
    }
}
