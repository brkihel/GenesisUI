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
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Theme.ThemeRuntime), typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeTokens))]
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
        public static Image Dress(RectTransform rt, ThemeRuntime theme, string sprite, string panel, float fitHeight = 0f, bool background = true)
        {
            var frameSprite = theme.Sprite(sprite);
            float multiplier = CanvasScale(rt);
            if (fitHeight > 0f && frameSprite != null)
                multiplier *= theme.Size(sprite).y / fitHeight;

            if (background) Background(rt, theme, sprite, panel, multiplier);
            var frame = Ui.Image(Ui.Fill(Ui.Child(rt, "Frame")), frameSprite,
                frameSprite != null ? Color.white : ThemeRuntime.ToUnity(theme.Tokens.PanelBackground));
            frame.pixelsPerUnitMultiplier = multiplier;
            return frame;
        }

        /// <summary>
        /// Sliced and tiled sprites are sized by the canvas's reference pixels per unit: our sprites are
        /// authored for 100 (200 px per unit at 2x, 2 px per design unit). A canvas with another
        /// reference (Jötunn's GUI canvases use 50) draws every 9-slice border at another size, which
        /// made the window bars look stretched (R-046, R-048). This is the multiplier that keeps borders
        /// at their design size on any canvas.
        /// </summary>
        public static float CanvasScale(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            if (canvas == null) return 1f;
            float reference = canvas.rootCanvas.referencePixelsPerUnit;
            return reference > 0f ? reference / 100f : 1f;
        }

        /// <summary>The root canvas's reference pixels per unit, for the logs.</summary>
        public static float CanvasReference(Transform t)
        {
            var canvas = t.GetComponentInParent<Canvas>();
            return canvas != null ? canvas.rootCanvas.referencePixelsPerUnit : -1f;
        }

        /// <summary>The panel material clipped to the frame's silhouette; null when the art has none.</summary>
        public static Image Background(RectTransform rt, ThemeRuntime theme, string sprite, string panel, float multiplier = -1f)
        {
            if (multiplier <= 0f) multiplier = CanvasScale(rt);
            var shape = theme.Sprite(sprite + "_shape");
            var material = theme.Sprite("panel_bg");
            if (shape == null || material == null) return null;

            var back = Ui.Fill(Ui.Child(rt, "Background"));
            var clip = Ui.Image(back, shape, Color.white);
            clip.pixelsPerUnitMultiplier = multiplier;
            back.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var tiled = Ui.Image(Ui.Fill(Ui.Child(back, "Material")), material, Color.white);
            tiled.type = Image.Type.Tiled;
            tiled.pixelsPerUnitMultiplier = CanvasScale(rt);
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
