using GenesisUI.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>Small helpers to build uGUI objects from code with theme fonts and sprites.</summary>
    internal static class Ui
    {
        public static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchors and pivots at one corner, then sets position and size in UI units.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Fill(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(RectTransform rt, Sprite sprite, Color color, bool raycast = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            // Thin-line frames are lit by the metal shader when it is available (D-033).
            ThemeRuntime.Current?.Metalize(img);
            return img;
        }

        /// <summary>
        /// A text on its own child object. The object is built inactive and enabled after
        /// the font is set: TMP's Awake otherwise looks for LiberationSans (not in Valheim)
        /// and warns.
        /// </summary>
        public static TextMeshProUGUI Text(Transform parent, string name, ThemeRuntime theme, FontRole role, float size,
                                           Color color, TextAlignmentOptions alignment, bool outlined = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.SetActive(false);
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            var font = theme.Font(role);
            if (font != null) text.font = font;
            if (outlined)
            {
                var mat = theme.OutlinedMaterial(role);
                if (mat != null) text.fontSharedMaterial = mat;
            }
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;

            go.SetActive(true);
            return text;
        }

        /// <summary>
        /// Shrinks the text to fit its box instead of spilling over a frame (R-030: numbers such as
        /// "148" overlapped the narrow bar frames). Never grows above the given size.
        /// </summary>
        public static TextMeshProUGUI Fit(TextMeshProUGUI text, float minSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = Mathf.Min(minSize, text.fontSize);
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }
    }
}
