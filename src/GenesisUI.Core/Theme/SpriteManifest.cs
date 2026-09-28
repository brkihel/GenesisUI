using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GenesisUI.Theme
{
    /// <summary>
    /// art/sprites.json as written by tools/art/render.py. Public fields, because the
    /// plugin parses it with Unity's JsonUtility (docs/DECISIONS.md D-013).
    /// </summary>
    [Serializable]
    public sealed class SpriteManifest
    {
        public int scale;
        public SpriteEntry[] sprites;
    }

    [Serializable]
    public sealed class SpriteEntry
    {
        public string name;
        public string file;
        public int width;
        public int height;
        public int borderLeft;
        public int borderBottom;
        public int borderRight;
        public int borderTop;
    }

    /// <summary>
    /// Rejects anything in the manifest that could point outside the art folder or
    /// build a nonsense sprite. The manifest ships with the mod, but a player can edit
    /// it, and data must never become a path we did not choose (AGENTS.md rule 10).
    /// </summary>
    public static class SpriteManifestValidator
    {
        public const int MaxSprites = 256;
        public const int MaxDesignSize = 2048;
        private static readonly Regex Name = new Regex("^[a-z0-9_]{1,48}$", RegexOptions.CultureInvariant);

        public static IReadOnlyList<string> Validate(SpriteManifest m)
        {
            var errors = new List<string>();
            if (m == null) { errors.Add("manifest is empty or not JSON"); return errors; }
            if (m.scale < 1 || m.scale > 4) errors.Add("scale must be 1-4, got " + m.scale);
            if (m.sprites == null || m.sprites.Length == 0) { errors.Add("no sprites"); return errors; }
            if (m.sprites.Length > MaxSprites) errors.Add("too many sprites: " + m.sprites.Length);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in m.sprites)
            {
                if (s == null) { errors.Add("null sprite entry"); continue; }
                string id = s.name ?? "(no name)";
                if (s.name == null || !Name.IsMatch(s.name)) errors.Add(id + ": name must match [a-z0-9_]{1,48}");
                else if (!seen.Add(s.name)) errors.Add(id + ": duplicate name");
                if (s.file != s.name + ".png") errors.Add(id + ": file must be exactly '<name>.png'");
                if (s.width < 1 || s.width > MaxDesignSize || s.height < 1 || s.height > MaxDesignSize) errors.Add(id + ": size out of range");
                if (s.borderLeft < 0 || s.borderRight < 0 || s.borderTop < 0 || s.borderBottom < 0) errors.Add(id + ": negative border");
                if (s.borderLeft + s.borderRight > s.width || s.borderTop + s.borderBottom > s.height) errors.Add(id + ": borders larger than the sprite");
            }
            return errors;
        }
    }
}
