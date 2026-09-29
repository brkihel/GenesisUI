using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// One layer of a bar's liquid: Diego's texture (looped by tools/art/sheets.py) on a RawImage
    /// whose UVs drift (AGENTS.md §2b). The bar's level is decided by the mask above it, never by
    /// this layer, so motion and level stay independent. Allocation-free per frame.
    /// </summary>
    internal sealed class LiquidLayer
    {
        public readonly RawImage Image;
        private readonly Vector2 _speed;

        /// <param name="area">The layer's size in design units.</param>
        /// <param name="texture">The texture's design size (art/sprites.json).</param>
        /// <param name="density">Texture design units shown per design unit of the bar (above 1: finer detail).</param>
        /// <param name="speed">
        /// UV drift per second (u, v); positive v moves the liquid up. Drift only along the axis the
        /// texture loops on (v for the vertical liquids, u for the <c>_h</c> ones).
        /// </param>
        /// <param name="mirror">Flip horizontally: a second layer never lines up with the first.</param>
        public LiquidLayer(RectTransform rt, Texture texture, Vector2 area, Vector2 textureSize, float density, Color color,
                           Vector2 speed, Vector2 offset, bool mirror)
        {
            Image = rt.gameObject.AddComponent<RawImage>();
            Image.texture = texture;
            Image.color = color;
            Image.raycastTarget = false;
            float w = textureSize.x > 0f ? area.x * density / textureSize.x : 1f;
            float h = textureSize.y > 0f ? area.y * density / textureSize.y : 1f;
            // Across the non-looping axis the window must stay inside the texture: offset.x + w <= 1 there.
            Image.uvRect = mirror ? new Rect(offset.x + w, offset.y, -w, h) : new Rect(offset.x, offset.y, w, h);
            _speed = speed;
        }

        public void Scroll(float deltaSeconds)
        {
            // Repeat keeps the offsets bounded (R-040: an unbounded clock striped the liquid).
            var uv = Image.uvRect;
            if (_speed.y != 0f) uv.y = Mathf.Repeat(uv.y - _speed.y * deltaSeconds, 1f);
            if (_speed.x != 0f) uv.x = Mathf.Repeat(uv.x + _speed.x * deltaSeconds, 1f);
            Image.uvRect = uv;
        }
    }
}
