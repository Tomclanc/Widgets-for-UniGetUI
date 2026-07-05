using System.Diagnostics;

namespace WidgetsForUniGetUI
{
    internal static class Logger
    {
        public static void Log(string s)
        {
            WriteToFile(s);
            TryConsoleWrite(s);
            TryDebugWrite(s);
        }

        public static void Log(Exception e)
        {
            Log(e.ToString());
        }
        public static void Log(int i)
        {
            Log(i.ToString());
        }

        private static void WriteToFile(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            foreach (string? logPath in GetLogPaths())
            {
                if (string.IsNullOrWhiteSpace(logPath))
                {
                    continue;
                }

                try
                {
                    string? logDir = Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrWhiteSpace(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    File.AppendAllText(logPath, line);
                }
                catch
                {
                    // Logging must never interfere with widget activation.
                }
            }
        }

        private static IEnumerable<string?> GetLogPaths()
        {
            string? localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                yield return Path.Join(localAppData, "Widgets-for-UniGetUI", "widget.log");
            }

            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                yield return Path.Join(userProfile, "AppData", "Local", "Widgets-for-UniGetUI", "widget.log");
            }

            string temp = Path.GetTempPath();
            if (!string.IsNullOrWhiteSpace(temp))
            {
                yield return Path.Join(temp, "Widgets-for-UniGetUI-widget.log");
            }

            yield return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Widgets-for-UniGetUI-widget.log");
        }

        private static void TryConsoleWrite(string message)
        {
            try
            {
                Console.WriteLine(message);
            }
            catch
            {
            }
        }

        private static void TryDebugWrite(string message)
        {
            try
            {
                Debug.WriteLine(message);
            }
            catch
            {
            }
        }
    }
}
