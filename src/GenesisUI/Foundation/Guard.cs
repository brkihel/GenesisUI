using System;
using System.Diagnostics;
using GenesisUI.Foundation.Faults;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Runs owner-scoped code so that a failure stays local (docs/ARCHITECTURE.md §3).
    /// A throwing owner is recorded, logged once with its full stack, and tripped:
    /// further calls for it are skipped until an explicit reset.
    /// </summary>
    internal static class Guard
    {
        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        public static readonly FaultRegistry Faults = new FaultRegistry();

        public static double Now => Clock.Elapsed.TotalSeconds;

        public static bool IsTripped(string owner) => Faults.IsTripped(owner);

        /// <returns>True if the action ran to completion.</returns>
        public static bool Run(string owner, Action action)
        {
            if (Faults.IsTripped(owner)) return false;
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Fault(owner, e);
                return false;
            }
        }

        /// <summary>
        /// Allocation-free variant for per-frame calls: pass a cached delegate and its
        /// argument instead of a capturing lambda.
        /// </summary>
        public static bool Run<TArg>(string owner, Action<TArg> action, TArg arg)
        {
            if (Faults.IsTripped(owner)) return false;
            try
            {
                action(arg);
                return true;
            }
            catch (Exception e)
            {
                Fault(owner, e);
                return false;
            }
        }

        /// <summary>
        /// For clean-up paths that must always be attempted (teardown, restore, config
        /// toggles): logs a failure but never trips, so the next attempt still runs.
        /// </summary>
        public static bool Try(string what, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                GenesisLog.Error("Guard", what + " failed: " + e, "try:" + what);
                return false;
            }
        }

        public static T Run<T>(string owner, Func<T> func, T fallback)
        {
            if (Faults.IsTripped(owner)) return fallback;
            try
            {
                return func();
            }
            catch (Exception e)
            {
                Fault(owner, e);
                return fallback;
            }
        }

        /// <summary>Records a fault caught elsewhere (e.g. by the patcher).</summary>
        public static void Fault(string owner, Exception e) => Fault(owner, e.GetType().Name + ": " + e.Message, e.ToString());

        public static void Fault(string owner, string message, string detail)
        {
            try
            {
                var record = Faults.Report(owner, message, detail, Now);
                if (record.Count == 1)
                    GenesisLog.Error("Guard", owner + " faulted and was switched off for this session: " + detail);
                else
                    GenesisLog.Error("Guard", owner + " faulted again (" + record.Count + " total): " + message, "fault:" + owner);
            }
            catch
            {
                // The guard must never throw into the code it protects.
            }
        }
    }
}
