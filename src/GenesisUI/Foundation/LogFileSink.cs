using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Own log file for diagnostics builds: BepInEx/&lt;Product&gt;/logs/&lt;product&gt;-yyyyMMdd-HHmmss.log.
    /// Keeps the newest files only and stops at a size cap, so a runaway fault can
    /// never fill the player's disk (docs/DIAGNOSTICS.md §2).
    /// </summary>
    internal sealed class LogFileSink : IDisposable
    {
        private const long MaxBytes = 2L * 1024 * 1024;
        private const int KeepFiles = 5;

        private StreamWriter _writer;
        private long _written;
        private bool _capped;

        public string FilePath { get; }

        private LogFileSink(string path, StreamWriter writer)
        {
            FilePath = path;
            _writer = writer;
        }

        /// <summary>Returns null (and no file) if the folder cannot be written; logging then continues without it.</summary>
        public static LogFileSink TryOpen(string directory, string filePrefix)
        {
            try
            {
                Directory.CreateDirectory(directory);
                PruneOldFiles(directory, filePrefix);
                string path = Path.Combine(directory, filePrefix + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + ".log");
                var writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
                return new LogFileSink(path, writer);
            }
            catch
            {
                return null;
            }
        }

        public void WriteLine(string line)
        {
            if (_writer == null || _capped) return;
            try
            {
                _written += line.Length + 1;
                if (_written > MaxBytes)
                {
                    _capped = true;
                    _writer.WriteLine("-- log size cap reached (" + MaxBytes / 1024 + " KB); further lines only go to BepInEx/LogOutput.log --");
                    return;
                }
                _writer.WriteLine(line);
            }
            catch
            {
                _capped = true;
            }
        }

        public void Dispose()
        {
            try { _writer?.Dispose(); } catch { }
            _writer = null;
        }

        private static void PruneOldFiles(string directory, string filePrefix)
        {
            var old = new DirectoryInfo(directory).GetFiles(filePrefix + "-*.log")
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .Skip(KeepFiles - 1); // leave room for the file about to be created
            foreach (var f in old)
            {
                try { f.Delete(); } catch { }
            }
        }
    }
}
