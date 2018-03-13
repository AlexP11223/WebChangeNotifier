using System;
using System.IO;
using WebChangeNotifier.Helpers;

namespace WebChangeNotifier
{
    public class Logger
    {
        private static Logger _inst;

        private readonly string _filePath;

        private bool _reportedFileError;

        public Logger(string filePath = null)
        {
            _filePath = filePath;
        }

        public static string GenerateFileName() => $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss-ms}_{Guid.NewGuid().ToString().Substring(0, 4)}";

        public static void Init(string filePath = null)
        {
            _inst = new Logger(filePath);
        }

        public static void Log(string text)
        {
            _inst.Append(text);
        }

        public void Append(string text)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");

            try
            {
                WriteFile(text);

                _reportedFileError = false;
            }
            catch (Exception ex)
            {
                if (!_reportedFileError)
                {
                    Console.WriteLine($"*** Log file I/O error: {ex}");
                }

                _reportedFileError = true;
            }
        }

        private void WriteFile(string text)
        {
            if (_filePath != null)
            {
                FileHelper.CreateDirectoryIfNotExist(_filePath);

                File.AppendAllText(_filePath, $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] {text}\r\n");
            }
        }
    }
}
