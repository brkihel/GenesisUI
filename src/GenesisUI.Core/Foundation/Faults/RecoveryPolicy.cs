using System.Collections.Generic;

namespace GenesisUI.Foundation.Faults
{
    /// <summary>
    /// Whether a faulted module is rebuilt at once (Diego: a broken window must never stay broken, and
    /// the player must not have to press "re-enable"). A module that keeps failing would loop, so each
    /// owner gets <see cref="MaxRestarts"/> automatic restarts within <see cref="WindowSeconds"/>;
    /// after that it stays off (vanilla's own look) until the game restarts or the player retries.
    /// </summary>
    public sealed class RecoveryPolicy
    {
        public const int MaxRestarts = 3;
        public const double WindowSeconds = 300.0;

        private readonly Dictionary<string, List<double>> _restarts = new Dictionary<string, List<double>>();

        /// <summary>Records and allows a restart now, or refuses one (too many recent restarts).</summary>
        public bool TryRestart(string owner, double nowSeconds)
        {
            if (!_restarts.TryGetValue(owner, out var times)) _restarts[owner] = times = new List<double>();
            times.RemoveAll(t => nowSeconds - t > WindowSeconds);
            if (times.Count >= MaxRestarts) return false;
            times.Add(nowSeconds);
            return true;
        }

        /// <summary>Restarts left for an owner right now.</summary>
        public int Left(string owner, double nowSeconds)
        {
            if (!_restarts.TryGetValue(owner, out var times)) return MaxRestarts;
            int recent = 0;
            foreach (var t in times) if (nowSeconds - t <= WindowSeconds) recent++;
            return MaxRestarts - recent < 0 ? 0 : MaxRestarts - recent;
        }

        public void Forget(string owner) => _restarts.Remove(owner);
    }
}
