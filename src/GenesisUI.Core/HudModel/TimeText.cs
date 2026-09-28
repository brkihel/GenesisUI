using System;

namespace GenesisUI.HudModel
{
    /// <summary>How a remaining time is written on a tile, without allocating.</summary>
    public readonly struct TimeText
    {
        public readonly int Value;
        /// <summary>'m' for minutes, 's' for seconds, ':' for m:ss (Value = total seconds).</summary>
        public readonly char Unit;
        /// <summary>The last minute: the view blinks the text, as vanilla does.</summary>
        public readonly bool Urgent;

        public TimeText(int value, char unit, bool urgent)
        {
            Value = value;
            Unit = unit;
            Urgent = urgent;
        }

        /// <summary>
        /// Food time exactly as vanilla Hud.UpdateFood writes it: from 60 s up, whole
        /// minutes rounded up ("12m"); below, whole seconds rounded down ("45s"), urgent.
        /// </summary>
        public static TimeText Food(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f) seconds = 0f;
            if (seconds >= 60f) return new TimeText((int)Math.Ceiling(seconds / 60f), 'm', false);
            return new TimeText((int)Math.Floor(seconds), 's', true);
        }

        /// <summary>Cooldowns as m:ss (Value = whole seconds, rounded up so "0:00" means ready).</summary>
        public static TimeText Clock(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f) seconds = 0f;
            int total = (int)Math.Ceiling(seconds);
            return new TimeText(total, ':', total > 0 && total < 60);
        }

        public int Minutes => Value / 60;
        public int Seconds => Value % 60;
    }
}
