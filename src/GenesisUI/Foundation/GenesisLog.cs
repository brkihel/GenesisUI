using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Logging;
using GenesisUI.Foundation.Logging;

namespace GenesisUI.Foundation
{
    internal enum LogSeverity
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    /// <summary>
    /// Category logging with rate limiting, a recent-lines buffer for reports and,
    /// in diagnostics builds, an own log file (docs/DIAGNOSTICS.md §2).
    /// Safe to call before Init (lines are kept in the buffer only) and from any thread.
    /// </summary>
    internal static class GenesisLog
    {
        private const int RecentCapacity = 200;

        private static readonly object Lock = new object();
        private static readonly Queue<string> RecentLines = new Queue<string>(RecentCapacity);
        private static readonly RateLimiter Limiter = new RateLimiter(10.0);
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        private static ManualLogSource _source;
        private static string _prefix = "Genesis";
        private static LogSeverity _minimumToBepInEx = LogSeverity.Info;
        private static LogFileSink _file;

        public static void Init(ManualLogSource source, string product, LogSeverity minimumToBepInEx, LogFileSink file)
        {
            lock (Lock)
            {
                _source = source;
                _prefix = product;
                _minimumToBepInEx = minimumToBepInEx;
                _file = file;
            }
        }

        public static void Debug(string category, string message, string rateKey = null) => Write(LogSeverity.Debug, category, message, rateKey);
        public static void Info(string category, string message, string rateKey = null) => Write(LogSeverity.Info, category, message, rateKey);
        public static void Warn(string category, string message, string rateKey = null) => Write(LogSeverity.Warning, category, message, rateKey);
        public static void Error(string category, string message, string rateKey = null) => Write(LogSeverity.Error, category, message, rateKey);

        public static IReadOnlyList<string> Recent()
        {
            lock (Lock) return RecentLines.ToArray();
        }

        public static void Shutdown()
        {
            lock (Lock)
            {
                _file?.Dispose();
                _file = null;
            }
        }

        private static void Write(LogSeverity severity, string category, string message, string rateKey)
        {
            try
            {
                if (rateKey != null)
                {
                    if (!Limiter.ShouldEmit(rateKey, Clock.Elapsed.TotalSeconds, out int suppressed)) return;
                    if (suppressed > 0) message += " (+" + suppressed + " suppressed)";
                }

                string line = "[" + _prefix + ":" + category + "] " + message;
                string stamped = DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)
                                 + " " + severity.ToString().ToUpperInvariant().PadRight(7) + " " + line;

                ManualLogSource source;
                LogSeverity minimum;
                lock (Lock)
                {
                    if (RecentLines.Count == RecentCapacity) RecentLines.Dequeue();
                    RecentLines.Enqueue(stamped);
                    _file?.WriteLine(stamped);
                    source = _source;
                    minimum = _minimumToBepInEx;
                }

                if (source == null || severity < minimum) return;
                switch (severity)
                {
                    case LogSeverity.Debug: source.LogDebug(line); break;
                    case LogSeverity.Info: source.LogInfo(line); break;
                    case LogSeverity.Warning: source.LogWarning(line); break;
                    default: source.LogError(line); break;
                }
            }
            catch
            {
                // Logging must never be the reason a game hook throws.
            }
        }
    }
}
