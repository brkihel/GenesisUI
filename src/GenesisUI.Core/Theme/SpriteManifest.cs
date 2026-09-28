using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GenesisUI.Data;

namespace GenesisUI.Theme
{
    /// <summary>art/sprites.json as written by tools/art/render.py.</summary>
    public sealed class SpriteManifest
    {
        public int scale;
        public SpriteEntry[] sprites;
    }

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

        /// <summary>"clamp" (default) or "repeat" for textures that scroll or tile (animated patterns).</summary>
        public string wrap = "clamp";

        /// <summary>
        /// Content insets in design units: where a frame's contents go (a bar's liquid, a
        /// plate's track). -1 = not declared; code then uses its own default. Each art style
        /// declares its own, so layout never hard-codes one style's measurements.
        /// </summary>
        public int contentLeft = -1;
        public int contentBottom = -1;
        public int contentRight = -1;
        public int contentTop = -1;

        public bool HasContent => contentLeft >= 0 && contentBottom >= 0 && contentRight >= 0 && contentTop >= 0;
    }

    /// <summary>
    /// Rejects anything in the manifest that could point outside the art folder or
    /// build a nonsense sprite. The manifest ships with the mod, but a player can edit
    /// it, and data must never become a path we did not choose (AGENTS.md rule 10).
    /// </summary>
    public static class SpriteManifestValidator
    {
        /// <summary>
        /// Reads the manifest with <see cref="StrictJson"/> (D-018). Unknown properties are
        /// errors, not silently ignored: a typo must not produce a sprite with no border.
        /// </summary>
        public static SpriteManifest Parse(string json, List<string> errors)
        {
            object root;
            try { root = StrictJson.Parse(json); }
            catch (FormatException e) { errors.Add(e.Message); return null; }

            if (!(root is Dictionary<string, object> obj)) { errors.Add("manifest must be a JSON object"); return null; }
            var m = new SpriteManifest();
            foreach (var kv in obj)
            {
                switch (kv.Key)
                {
                    case "scale": m.scale = Int(kv.Value, "scale", errors); break;
                    case "sprites":
                        if (!(kv.Value is List<object> list)) { errors.Add("sprites must be an array"); break; }
                        m.sprites = new SpriteEntry[list.Count];
                        for (int i = 0; i < list.Count; i++) m.sprites[i] = Entry(list[i], i, errors);
                        break;
                    default: errors.Add("unknown property '" + kv.Key + "'"); break;
                }
            }
            return m;
        }

        private static SpriteEntry Entry(object value, int index, List<string> errors)
        {
            if (!(value is Dictionary<string, object> obj)) { errors.Add("sprites[" + index + "] must be an object"); return null; }
            var e = new SpriteEntry();
            foreach (var kv in obj)
            {
                string where = "sprites[" + index + "]." + kv.Key;
                switch (kv.Key)
                {
                    case "name": e.name = kv.Value as string; if (e.name == null) errors.Add(where + " must be a string"); break;
                    case "file": e.file = kv.Value as string; if (e.file == null) errors.Add(where + " must be a string"); break;
                    case "width": e.width = Int(kv.Value, where, errors); break;
                    case "height": e.height = Int(kv.Value, where, errors); break;
                    case "borderLeft": e.borderLeft = Int(kv.Value, where, errors); break;
                    case "borderBottom": e.borderBottom = Int(kv.Value, where, errors); break;
                    case "borderRight": e.borderRight = Int(kv.Value, where, errors); break;
                    case "borderTop": e.borderTop = Int(kv.Value, where, errors); break;
                    case "wrap": e.wrap = kv.Value as string; if (e.wrap == null) errors.Add(where + " must be a string"); break;
                    case "contentLeft": e.contentLeft = Int(kv.Value, where, errors); break;
                    case "contentBottom": e.contentBottom = Int(kv.Value, where, errors); break;
                    case "contentRight": e.contentRight = Int(kv.Value, where, errors); break;
                    case "contentTop": e.contentTop = Int(kv.Value, where, errors); break;
                    default: errors.Add("unknown property '" + where + "'"); break;
                }
            }
            return e;
        }

        private static int Int(object value, string where, List<string> errors)
        {
            if (value is double d && d == Math.Floor(d) && d >= int.MinValue && d <= int.MaxValue) return (int)d;
            errors.Add(where + " must be an integer");
            return 0;
        }

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
                if (s.wrap != "clamp" && s.wrap != "repeat") errors.Add(id + ": wrap must be 'clamp' or 'repeat'");
                bool anyContent = s.contentLeft >= 0 || s.contentBottom >= 0 || s.contentRight >= 0 || s.contentTop >= 0;
                if (anyContent && !s.HasContent) errors.Add(id + ": content insets must be given all four or none");
                if (s.HasContent && (s.contentLeft + s.contentRight >= s.width || s.contentTop + s.contentBottom >= s.height))
                    errors.Add(id + ": content insets leave no room");
            }
            return errors;
        }
    }
}
