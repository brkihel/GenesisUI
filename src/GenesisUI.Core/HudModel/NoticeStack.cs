using System;
using System.Collections.Generic;

namespace GenesisUI.HudModel
{
    /// <summary>
    /// The top-left notices as a short stack (Diego, R-040): up to <see cref="Capacity"/> at a time,
    /// newest on top; each stays <see cref="ShowSeconds"/>, then fades while dropping down and gives
    /// its place to the others. Vanilla shows one message at a time and replaces it within a second
    /// when several arrive, which read as flicker. A repeat of the top notice within
    /// <see cref="MergeSeconds"/> ("Madeira x5" after "Madeira") updates it instead of stacking,
    /// as vanilla merges it. Pure logic; the view reads <see cref="Items"/> every frame.
    /// </summary>
    public sealed class NoticeStack
    {
        public const int Capacity = 3;
        public const float ShowSeconds = 4f;
        public const float MergeSeconds = 4f;
        public const float FadeInSeconds = 0.15f;
        public const float LeaveSeconds = 0.45f;

        public sealed class Notice
        {
            public string Text;
            /// <summary>Opaque icon key (the view's sprite); compared by reference.</summary>
            public object Icon;
            public float Age;
            public bool Leaving;
            public float LeaveTime;
            /// <summary>Changes when the text changes, so the view re-lays the card only then.</summary>
            public int Version;

            /// <summary>0..1 opacity: quick fade-in, fade-out while leaving.</summary>
            public float Alpha => Leaving ? 1f - Clamp01(LeaveTime / LeaveSeconds)
                                          : Clamp01(Age / FadeInSeconds);

            /// <summary>0..1 of the drop while leaving (eased), for the view to scale to a distance.</summary>
            public float Drop
            {
                get
                {
                    if (!Leaving) return 0f;
                    float t = Clamp01(LeaveTime / LeaveSeconds);
                    return t * t * (3f - 2f * t);
                }
            }
        }

        private readonly List<Notice> _items = new List<Notice>();

        /// <summary>Newest first. Leaving notices stay in place until they are gone.</summary>
        public IReadOnlyList<Notice> Items => _items;

        /// <returns>True when a new notice was added; false when the top one was updated.</returns>
        public bool Push(string text, object icon)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (_items.Count > 0)
            {
                var top = _items[0];
                if (!top.Leaving && top.Age < MergeSeconds && ReferenceEquals(top.Icon, icon) && SameBase(top.Text, text))
                {
                    if (!string.Equals(top.Text, text, StringComparison.Ordinal)) { top.Text = text; top.Version++; }
                    top.Age = Math.Min(top.Age, FadeInSeconds);
                    return false;
                }
            }

            _items.Insert(0, new Notice { Text = text, Icon = icon });
            int staying = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Leaving) continue;
                if (++staying > Capacity) _items[i].Leaving = true;
            }
            return true;
        }

        public void Tick(float dt)
        {
            if (dt < 0f || float.IsNaN(dt)) return;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var n = _items[i];
                n.Age += dt;
                if (!n.Leaving && n.Age >= ShowSeconds) n.Leaving = true;
                else if (n.Leaving) n.LeaveTime += dt;
                if (n.Leaving && n.LeaveTime >= LeaveSeconds) _items.RemoveAt(i);
            }
        }

        public void Clear() => _items.Clear();

        /// <summary>
        /// True when two notice texts differ only by vanilla's " xN" amount suffix
        /// ("Madeira" / "Madeira x5" / "Madeira x10").
        /// </summary>
        public static bool SameBase(string a, string b)
        {
            if (a == null || b == null) return false;
            int la = BaseLength(a), lb = BaseLength(b);
            return la == lb && string.CompareOrdinal(a, 0, b, 0, la) == 0;
        }

        private static int BaseLength(string s)
        {
            int i = s.Length - 1;
            while (i >= 0 && s[i] >= '0' && s[i] <= '9') i--;
            bool hasDigits = i < s.Length - 1;
            if (hasDigits && i >= 1 && s[i] == 'x' && s[i - 1] == ' ') return i - 1;
            return s.Length;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
