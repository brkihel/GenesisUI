using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// Reversible changes to vanilla UI objects (D-025): layout, images, enabled flags and opacity
    /// groups. Each object's original values are recorded the first time it is touched; Restore puts
    /// every one back exactly, in reverse order, and removes the groups we added. Nothing is ever
    /// re-parented, destroyed or deactivated. Self-contained (it will move to GenesisModLIB).
    /// </summary>
    internal sealed class VanillaSkin
    {
        private readonly string _owner;
        private readonly List<Action> _restore = new List<Action>(128);
        private readonly HashSet<int> _recorded = new HashSet<int>();

        public VanillaSkin(string owner)
        {
            _owner = owner;
        }

        public int Count => _restore.Count;

        /// <summary>Records a RectTransform's layout before the caller moves or resizes it.</summary>
        public RectTransform Rect(RectTransform rt)
        {
            if (rt == null || !_recorded.Add(Key(rt, 1))) return rt;
            var anchorMin = rt.anchorMin;
            var anchorMax = rt.anchorMax;
            var pivot = rt.pivot;
            var position = rt.anchoredPosition;
            var size = rt.sizeDelta;
            var scale = rt.localScale;
            _restore.Add(() =>
            {
                if (rt == null) return;
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.pivot = pivot;
                rt.anchoredPosition = position;
                rt.sizeDelta = size;
                rt.localScale = scale;
            });
            return rt;
        }

        /// <summary>Records an Image's look (sprite, type, colour, multiplier, enabled) before it is restyled.</summary>
        public Image Image(Image image)
        {
            if (image == null || !_recorded.Add(Key(image, 2))) return image;
            var sprite = image.sprite;
            var type = image.type;
            var color = image.color;
            var multiplier = image.pixelsPerUnitMultiplier;
            var enabled = image.enabled;
            var preserve = image.preserveAspect;
            _restore.Add(() =>
            {
                if (image == null) return;
                image.sprite = sprite;
                image.type = type;
                image.color = color;
                image.pixelsPerUnitMultiplier = multiplier;
                image.enabled = enabled;
                image.preserveAspect = preserve;
            });
            return image;
        }

        /// <summary>Records a text's font, material, colour and size before it is restyled.</summary>
        public TMPro.TMP_Text Text(TMPro.TMP_Text text)
        {
            if (text == null || !_recorded.Add(Key(text, 7))) return text;
            var font = text.font;
            var material = text.fontSharedMaterial;
            var color = text.color;
            var size = text.fontSize;
            var style = text.fontStyle;
            _restore.Add(() =>
            {
                if (text == null) return;
                text.font = font;
                text.fontSharedMaterial = material;
                text.color = color;
                text.fontSize = size;
                text.fontStyle = style;
            });
            return text;
        }

        /// <summary>Records a vanilla button's visual transition while its slot is dressed.</summary>
        public Button Button(Button button)
        {
            if (button == null || !_recorded.Add(Key(button, 6))) return button;
            var colors = button.colors;
            var transition = button.transition;
            _restore.Add(() =>
            {
                if (button == null) return;
                button.colors = colors;
                button.transition = transition;
            });
            return button;
        }

        /// <summary>Records a component's enabled flag.</summary>
        public T Enabled<T>(T behaviour) where T : Behaviour
        {
            if (behaviour == null || !_recorded.Add(Key(behaviour, 3))) return behaviour;
            bool enabled = behaviour.enabled;
            _restore.Add(() => { if (behaviour != null) behaviour.enabled = enabled; });
            return behaviour;
        }

        /// <summary>
        /// A CanvasGroup on the object to fade or isolate it: vanilla's own group when it has one
        /// (its values restored later), otherwise one of ours (removed later).
        /// </summary>
        public CanvasGroup Group(GameObject go)
        {
            if (go == null) return null;
            var group = go.GetComponent<CanvasGroup>();
            if (!_recorded.Add(Key(go, 4))) return group;
            if (group == null)
            {
                group = go.AddComponent<CanvasGroup>();
                var added = group;
                // Immediately: another owner may hide the same object in the same frame (a window
                // tab switch). A deferred Destroy let it find this dying group and reuse it, and
                // vanilla reappeared at the end of the frame (R-056).
                _restore.Add(() => { if (added != null) UnityEngine.Object.DestroyImmediate(added); });
            }
            else
            {
                var existing = group;
                float alpha = existing.alpha;
                bool raycasts = existing.blocksRaycasts;
                bool interactable = existing.interactable;
                bool ignore = existing.ignoreParentGroups;
                _restore.Add(() =>
                {
                    if (existing == null) return;
                    existing.alpha = alpha;
                    existing.blocksRaycasts = raycasts;
                    existing.interactable = interactable;
                    existing.ignoreParentGroups = ignore;
                });
            }
            return group;
        }

        /// <summary>
        /// Hides a vanilla object for as long as this skin holds it (Diego, 2026-10-02: fragments of
        /// vanilla showed through): its group at alpha 0 and taking no pointer, pinned there at the end of
        /// every frame — after vanilla's animators, which fade some panels' own groups back in — by a small
        /// component of ours, removed on restore. <paramref name="interactable"/> null leaves that flag
        /// as it is (a hidden field that must still take typing).
        /// </summary>
        public CanvasGroup Hidden(GameObject go, bool? interactable = false)
        {
            var group = Group(go);
            if (group == null) return null;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            if (interactable.HasValue) group.interactable = interactable.Value;
            if (go.GetComponent<HiddenPin>() == null) // one pin per object; removed on restore
            {
                var pin = go.AddComponent<HiddenPin>();
                pin.Group = group;
                _restore.Add(() => { if (pin != null) UnityEngine.Object.DestroyImmediate(pin); });
            }
            return group;
        }

        /// <summary>Keeps a hidden group hidden after anything else this frame (see <see cref="Hidden"/>).</summary>
        private sealed class HiddenPin : MonoBehaviour
        {
            internal CanvasGroup Group;

            private void LateUpdate()
            {
                if (Group == null) return;
                if (Group.alpha != 0f) Group.alpha = 0f;
                if (Group.blocksRaycasts) Group.blocksRaycasts = false;
            }
        }

        /// <summary>
        /// Stops an object from being drawn while it stays active and running: a disabled Canvas on it
        /// (vanilla's own, its flag restored later; or one of ours, removed later). Unlike a CanvasGroup's
        /// alpha, no animator can bring it back: vanilla's confirmation dialogs fade their group in.
        /// </summary>
        public void Undrawn(GameObject go)
        {
            if (go == null || !_recorded.Add(Key(go, 0))) return;
            var canvas = go.GetComponent<Canvas>();
            if (canvas != null)
            {
                bool enabled = canvas.enabled;
                var existing = canvas;
                _restore.Add(() => { if (existing != null) existing.enabled = enabled; });
            }
            else
            {
                canvas = go.AddComponent<Canvas>();
                var added = canvas;
                _restore.Add(() => { if (added != null) UnityEngine.Object.DestroyImmediate(added); });
            }
            canvas.enabled = false;
        }

        /// <summary>Records a renderer's alpha multiplier (vanilla resets colours, never this).</summary>
        public CanvasRenderer RendererAlpha(CanvasRenderer renderer)
        {
            if (renderer == null || !_recorded.Add(Key(renderer, 5))) return renderer;
            float alpha = renderer.GetAlpha();
            _restore.Add(() => { if (renderer != null) renderer.SetAlpha(alpha); });
            return renderer;
        }

        /// <summary>An object we added under a vanilla object: destroyed on restore.</summary>
        public GameObject Added(GameObject go)
        {
            if (go == null) return null;
            _restore.Add(() => { if (go != null) UnityEngine.Object.Destroy(go); });
            return go;
        }

        /// <summary>Puts every recorded object back, newest change first. Never throws.</summary>
        public int Restore()
        {
            int n = _restore.Count;
            for (int i = n - 1; i >= 0; i--)
            {
                var undo = _restore[i];
                Guard.Try(_owner + " restore", undo);
            }
            _restore.Clear();
            _recorded.Clear();
            if (n > 0) GenesisLog.Info("Skin", _owner + " restored " + n + " vanilla change(s)");
            return n;
        }

        private static int Key(UnityEngine.Object o, int kind) => o.GetInstanceID() * 8 + kind;
    }
}
