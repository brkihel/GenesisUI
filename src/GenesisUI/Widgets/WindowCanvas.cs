using BepInEx.Configuration;
using GenesisUI.Foundation;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Where GenesisUI's windows are drawn (R-048): on vanilla's own inventory canvas, beside
    /// InventoryGui — panels behind it, bars in front — so vanilla's slots, our frames and our buttons
    /// share one coordinate system, one scale and one raycaster. The window board (<see cref="Design"/>) is centred and
    /// scaled to <see cref="Size"/> of the screen's width (Diego: about 75%, R-049).
    /// </summary>
    internal static class WindowCanvas
    {
        internal static ConfigEntry<float> Size;
        internal static ConfigEntry<float> Height;

        internal static void Bind(ConfigFile config)
        {
            Size = config.Bind("Windows", "Width", 0.75f, new ConfigDescription(
                "Largura das janelas (inventário, criação...) em fração da tela: 0,75 = 75%.", new AcceptableValueRange<float>(0.45f, 1f)));
            Height = config.Bind("Windows", "Height", 0.75f, new ConfigDescription(
                "Altura máxima das janelas em fração da tela, das abas até as dicas de atalho (a proporção do concept é mantida).", new AcceptableValueRange<float>(0.5f, 1f)));
            // Preview.5 wrote 0.65 to existing configs. Move that exact old default once;
            // after the marker is set, a player can deliberately choose 0.65 again.
            var defaultsVersion = config.Bind("Windows", "ViewportDefaultsVersion", 0,
                "Versão interna da migração do tamanho padrão das janelas.");
            if (defaultsVersion.Value < 1)
            {
                if (Mathf.Approximately(Size.Value, 0.65f)) Size.Value = 0.75f;
                if (Mathf.Approximately(Height.Value, 0.65f)) Height.Value = 0.75f;
                defaultsVersion.Value = 1;
            }
        }

        /// <summary>
        /// A full-canvas root beside a vanilla UI object (InventoryGui, the build menu...): behind it or in
        /// front of it, on that object's own root canvas.
        /// </summary>
        internal static RectTransform CreateRoot(Component anchor, string name, bool behind)
        {
            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            var root = canvas.rootCanvas.transform;
            // The child of the canvas root that holds the anchor: our root goes right before or after it.
            var ancestor = anchor.transform;
            while (ancestor.parent != null && ancestor.parent != root) ancestor = ancestor.parent;
            var rt = Ui.Fill(Ui.Child(root, name));
            int index = ancestor.parent == root ? ancestor.GetSiblingIndex() : root.childCount - 1;
            rt.SetSiblingIndex(behind ? index : index + 1);
            if (behind)
            {
                // InventoryGrid has its own nested canvas in the game. Sibling order alone
                // cannot put our translucent panel below it: give the panel an explicit lower
                // sorting order so item icons, hit targets and hover highlights stay on top.
                var layer = rt.gameObject.AddComponent<Canvas>();
                layer.overrideSorting = true;
                layer.sortingLayerID = canvas.rootCanvas.sortingLayerID;
                layer.sortingOrder = Mathf.Max(short.MinValue, canvas.rootCanvas.sortingOrder - 1);
            }
            GenesisLog.Info("Windows", name + " on canvas '" + canvas.rootCanvas.name + "' (reference pixels per unit " +
                Frame.CanvasReference(rt).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", 9-slice scale " +
                Frame.CanvasScale(rt).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "), " + (behind ? "behind" : "in front of") +
                " " + anchor.GetType().Name);
            return rt;
        }

        /// <summary>
        /// The window's drawing board: ConceptArt (9)'s window measured in its own pixels (1580 x 850,
        /// tab bar to hint bar). Every window lays out in these units, like the concept, and the whole
        /// board is scaled uniformly to <see cref="Size"/> of the screen's width (or <see cref="Height"/>
        /// of its height, whichever is smaller): the proportions stay the concept's at any resolution.
        /// </summary>
        internal static readonly Vector2 Design = new Vector2(1580f, 850f);

        /// <summary>The centred design board inside a full-canvas root.</summary>
        internal static RectTransform Area(RectTransform root, string name)
        {
            var area = Ui.Child(root, name);
            area.anchorMin = area.anchorMax = area.pivot = new Vector2(0.5f, 0.5f);
            area.anchoredPosition = Vector2.zero;
            area.sizeDelta = Design;
            Fit(area);
            return area;
        }

        /// <summary>
        /// The windows' parallax (D-036), in design units at depth 1: set by the window shell from the
        /// pointer while a window is open, eased back to zero after. Each board moves by it times its
        /// depth (panels 1, the shell's bars less, the map's chrome not at all), so the layers separate.
        /// </summary>
        internal static Vector2 Parallax;

        /// <summary>Rescales the board to the current screen and places it with the parallax; cheap, called every refresh.</summary>
        internal static void Fit(RectTransform area, float depth = 1f)
        {
            var offset = Parallax * depth;
            if ((area.anchoredPosition - offset).sqrMagnitude > 0.0001f) area.anchoredPosition = offset;
            var parent = area.parent as RectTransform;
            if (parent == null) return;
            var screen = parent.rect.size;
            if (screen.x <= 0f || screen.y <= 0f) return;
            float w = Size != null ? Size.Value : 0.75f;
            float h = Height != null ? Height.Value : 0.75f;
            float scale = Mathf.Min(screen.x * w / Design.x, screen.y * h / Design.y);
            if (!Mathf.Approximately(area.localScale.x, scale)) area.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Places a child by its top-left corner in design units (y grows downwards, like the concept).</summary>
        internal static RectTransform At(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var rt = Ui.Child(parent, name);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }
    }
}
