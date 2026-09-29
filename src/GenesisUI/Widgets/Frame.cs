using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    internal enum Edge { Left, Right, Top, Bottom }

    /// <summary>
    /// A gold frame cut from Diego's sheets (D-027) with the panel material behind it
    /// (AGENTS.md §2b): the material is clipped to the frame's own silhouette
    /// (<c>&lt;name&gt;_shape</c>) and has its own opacity, per panel, from <c>[Backgrounds]</c>;
    /// the frame, texts and icons are never faded with it.
    /// </summary>
    internal static class Frame
    {
        /// <summary>
        /// Background (if the art has a shape for it) and frame, filling <paramref name="rt"/>.
        /// </summary>
        /// <param name="panel">The panel's key in <c>[Backgrounds]</c> (its opacity).</param>
        /// <param name="fitHeight">
        /// When above zero, the whole piece is drawn at this height's scale (corners and rails
        /// grow or shrink uniformly); only its plain straight rails stretch along the width.
        /// </param>
        public static Image Dress(RectTransform rt, ThemeRuntime theme, string sprite, string panel, float fitHeight = 0f)
        {
            var frameSprite = theme.Sprite(sprite);
            float multiplier = 1f;
            if (fitHeight > 0f && frameSprite != null)
                multiplier = theme.Size(sprite).y / fitHeight;

            Background(rt, theme, sprite, panel, multiplier);
            var frame = Ui.Image(Ui.Fill(Ui.Child(rt, "Frame")), frameSprite,
                frameSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground));
            frame.pixelsPerUnitMultiplier = multiplier;
            return frame;
        }

        /// <summary>The panel material clipped to the frame's silhouette; null when the art has none.</summary>
        public static Image Background(RectTransform rt, ThemeRuntime theme, string sprite, string panel, float multiplier = 1f)
        {
            var shape = theme.Sprite(sprite + "_shape");
            var material = theme.Sprite("panel_bg");
            if (shape == null || material == null) return null;

            var back = Ui.Fill(Ui.Child(rt, "Background"));
            var clip = Ui.Image(back, shape, Color.white);
            clip.pixelsPerUnitMultiplier = multiplier;
            back.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var tiled = Ui.Image(Ui.Fill(Ui.Child(back, "Material")), material, Color.white);
            tiled.type = Image.Type.Tiled;
            theme.RegisterBackground(tiled, panel);
            return tiled;
        }

        /// <summary>
        /// An ornament cut out of an edge's middle (it would stretch with the edge), put back on
        /// the rail at that edge's midpoint at its own size.
        /// </summary>
        public static Image Ornament(RectTransform frame, ThemeRuntime theme, string sprite, Edge edge, float scale = 1f)
        {
            var s = theme.Sprite(sprite);
            if (s == null) return null;
            float inset = theme.Inset(sprite) * scale;
            Vector2 anchor;
            Vector2 position;
            switch (edge)
            {
                case Edge.Left: anchor = new Vector2(0f, 0.5f); position = new Vector2(inset, 0f); break;
                case Edge.Right: anchor = new Vector2(1f, 0.5f); position = new Vector2(-inset, 0f); break;
                case Edge.Top: anchor = new Vector2(0.5f, 1f); position = new Vector2(0f, -inset); break;
                default: anchor = new Vector2(0.5f, 0f); position = new Vector2(0f, inset); break;
            }
            var rt = Ui.Place(Ui.Child(frame, sprite), anchor, position, theme.Size(sprite) * scale);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return Ui.Image(rt, s, Color.white);
        }
    }
}
