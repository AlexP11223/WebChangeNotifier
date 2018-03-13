using System;
using System.Diagnostics;
using System.IO;

namespace WebChangeNotifier.Helpers
{
    public class FileHelper
    {
        /// <summary>
        /// Creates directory if not exist.
        /// </summary>
        /// <param name="filePath">File path (C:\dir\file.txt) or directory path (C:\dir\).</param>
        public static void CreateDirectoryIfNotExist(string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);

            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// Deletes directory with all files inside
        /// </summary>
        /// <param name="dirPath">Directory path</param>
        public static void DeleteDirWithFiles(string dirPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dirPath);

            foreach (var file in dirInfo.GetFiles())
            {
                file.Delete();
            }

            Directory.Delete(dirPath);
        }

        /// <summary>
        /// Opens folder with the specified file in the Explorer and selects the file
        /// </summary>
        /// <param name="filePath">File path</param>
        public static void OpenFolder(string filePath)
        {
            Process.Start("explorer.exe", string.Format("/select,\"{0}\"", filePath));
        }
    }
}
