using GenesisUI.Theme;
using UnityEngine;
using UnityEngine.UI;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// What dims the world behind an open window: the GenesisUI/Backdrop shader's warm vignette with a
    /// faint grain, or the flat black veil it replaced when the shader is not loaded.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.Ui), typeof(GenesisUI.Theme.ThemeRuntime))]
    internal static class Backdrop
    {
        public static Graphic Create(RectTransform root, ThemeRuntime theme)
        {
            var rt = Ui.Fill(Ui.Child(root, "Dim"));
            var material = theme != null ? theme.BackdropMaterial : null;
            if (material == null) return Ui.Image(rt, null, new Color(0f, 0f, 0f, 0.35f));
            var image = rt.gameObject.AddComponent<RawImage>();
            image.raycastTarget = false;
            image.material = material;
            return image;
        }
    }
}
