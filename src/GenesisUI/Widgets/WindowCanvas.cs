using BepInEx.Configuration;
using GenesisUI.Foundation;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// Where GenesisUI's windows are drawn (R-048): on vanilla's own inventory canvas, beside
    /// InventoryGui — panels behind it, bars in front — so vanilla's slots, our frames and our buttons
    /// share one coordinate system, one scale and one raycaster. The window area is centred and takes
    /// <see cref="Size"/> of the screen's width (Diego: about 75%, R-049).
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
                "Altura das janelas em fração da tela, das abas até as dicas de atalho.", new AcceptableValueRange<float>(0.5f, 1f)));
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

        /// <summary>A full-canvas root beside InventoryGui: behind it (panels) or in front of it (bars).</summary>
        internal static RectTransform CreateRoot(InventoryGui gui, string name, bool behind)
        {
            var canvas = gui.GetComponentInParent<Canvas>();
            if (canvas == null) return null;
            var root = canvas.rootCanvas.transform;
            // The child of the canvas root that holds InventoryGui: our root goes right before or after it.
            var ancestor = gui.transform;
            while (ancestor.parent != null && ancestor.parent != root) ancestor = ancestor.parent;
            var rt = Ui.Fill(Ui.Child(root, name));
            int index = ancestor.parent == root ? ancestor.GetSiblingIndex() : root.childCount - 1;
            rt.SetSiblingIndex(behind ? index : index + 1);
            GenesisLog.Info("Windows", name + " on canvas '" + canvas.rootCanvas.name + "' (reference pixels per unit " +
                Frame.CanvasReference(rt).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", 9-slice scale " +
                Frame.CanvasScale(rt).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "), " + (behind ? "behind" : "in front of") +
                " InventoryGui");
            return rt;
        }

        /// <summary>The centred window area inside a full-canvas root.</summary>
        internal static RectTransform Area(RectTransform root, string name)
        {
            var area = Ui.Child(root, name);
            float w = Size != null ? Size.Value : 0.75f;
            float h = Height != null ? Height.Value : 0.75f;
            area.anchorMin = new Vector2((1f - w) / 2f, (1f - h) / 2f);
            area.anchorMax = new Vector2((1f + w) / 2f, (1f + h) / 2f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            return area;
        }
    }
}
