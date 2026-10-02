#if GENESIS_DIAGNOSTICS
using GenesisUI.Foundation;
using GenesisUI.Foundation.Versioning;
using Jotunn.Managers;
using TMPro;
using UnityEngine;

namespace GenesisUI.Diagnostics
{
    /// <summary>
    /// Bottom-right build label in every non-Release build, so any screenshot names
    /// the exact package it came from (docs/DIAGNOSTICS.md §1). Recreated on every
    /// scene change, because Jötunn recreates its GUI root with the scene.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Foundation.Guard))]
    internal static class Watermark
    {
        private const string Owner = "diag:watermark";
        private static string _text;
        private static GameObject _root;
        private static readonly System.Action Available = () => Guard.Run(Owner, Create);

        public static void Install(BuildChannel channel, string fullVersion)
        {
            _text = "GenesisUI " + (channel == BuildChannel.Preview ? "PREVIEW" : "DEV") + " " + fullVersion;
            GUIManager.OnCustomGUIAvailable -= Available;
            GUIManager.OnCustomGUIAvailable += Available;
        }

        private static void Create()
        {
            var root = GUIManager.CustomGUIFront;
            if (root == null) return;

            // Built inactive: TextMeshPro looks for its default font (LiberationSans, which
            // Valheim does not ship) in Awake, and warns before we can assign the game font.
            // Awake only runs on activation, after the font is set.
            var go = new GameObject("GenesisUI_Watermark", typeof(RectTransform));
            if (_root != null) Object.Destroy(_root);
            _root = go;
            go.SetActive(false);
            go.transform.SetParent(root.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 8f);
            rt.sizeDelta = new Vector2(700f, 24f);

            var label = go.AddComponent<TextMeshProUGUI>();
            if (GUIManager.Instance.TMP_AveriaSansLibre != null) label.font = GUIManager.Instance.TMP_AveriaSansLibre;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.BottomRight;
            label.color = new Color(0.98f, 0.81f, 0.45f, 0.55f); // accent.gold, see docs/ART-DIRECTION.md
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = _text;
            go.SetActive(true);

            GenesisLog.Debug("Diag", "watermark placed");
        }
        internal static void Shutdown()
        {
            GUIManager.OnCustomGUIAvailable -= Available;
            if (_root != null) Object.Destroy(_root);
            _root = null; _text = null;
        }
    }
}
#endif
