using System;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace TwitchBot
{
    public class Log : IDisposable
    {
        private bool toConsole;
        private StreamWriter writer;

        public Log(string logpath)
        {
            // Toggle console output depending on the build mode
#if DEBUG
            //toConsole = true;
            toConsole = false;
#else
            toConsole = false;
#endif

            // Now set up the file to output
            writer = new StreamWriter(new BufferedStream(File.OpenWrite(logpath)));
        }

        // Close function
        public void Close()
        {
            writer.Flush();
            writer.Close();
        }

        // Base logging function
        private void WriteLog(string file, int line, string level, string message)
        {
            string composed = String.Format("{0} {1}:{2} - {3}", level, file, line, message);

            if (toConsole)
                Console.WriteLine(composed);
            writer.WriteLine(composed);
        }

        // Externally visible shortcuts
        public void Error(string message, [CallerFilePath] string file="", [CallerLineNumber] int line=0)
        {
            WriteLog(file, line, "Error", message);
        }

        public void Warning(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(file, line, "Warning", message);
        }

        public void Info(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(file, line, "Info", message);
        }

        public void Debug(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            WriteLog(file, line, "Debug", message);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                writer.Flush();
                writer.Close();
            }
        }
    }
}
