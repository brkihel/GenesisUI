using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using GenesisUI.Foundation.Contracts;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Foundation
{
    /// <summary>
    /// The facts every log and report starts with (docs/DIAGNOSTICS.md §2): who we
    /// are, what game, what loader, what screen, and every other plugin present.
    /// Each game read is guarded individually: one missing value never hides the rest.
    /// </summary>
    [GameContract("assembly_valheim", "Version", "GetVersionString")]
    [GameContract("assembly_guiutils", "Localization", "GetSelectedLanguage")]
    [GameContract("assembly_guiutils", "Localization", "instance")]
    [GameContract("assembly_guiutils", "GuiScaler", "m_largeGuiScale")]
    internal static class SessionHeader
    {
        /// <param name="includeDisplay">
        /// False at plugin load: BepInEx runs before Valheim applies the player's
        /// resolution, so the window is still tiny (302x193 was logged on a 1920x1080
        /// screen). Display facts are logged later by <see cref="Display"/>.
        /// </param>
        public static IReadOnlyList<string> Build(string product, string fullVersion, string channel, bool includeDisplay)
        {
            var lines = new List<string>
            {
                product + " " + fullVersion + " (" + channel + ")",
                "Game: " + Read(() => global::Version.GetVersionString(false)),
                "Unity: " + Read(() => Application.unityVersion),
                "BepInEx: " + Read(() => typeof(BaseUnityPlugin).Assembly.GetName().Version.ToString()),
                "Jotunn: " + Read(() => Jotunn.Main.Version),
                "OS: " + Read(() => SystemInfo.operatingSystem),
                "Language: " + Read(() => Localization.instance?.GetSelectedLanguage() ?? "(not ready)"),
                "Plugins (" + Read(() => Chainloader.PluginInfos.Count.ToString()) + "):",
            };

            if (includeDisplay) lines.Insert(6, Display());

            try
            {
                lines.AddRange(Chainloader.PluginInfos.Values
                    .OrderBy(p => p.Metadata.GUID, StringComparer.OrdinalIgnoreCase)
                    .Select(p => "  " + p.Metadata.GUID + " " + p.Metadata.Version + " (" + p.Metadata.Name + ")"));
            }
            catch (Exception e)
            {
                lines.Add("  (plugin list unavailable: " + e.GetType().Name + ")");
            }

            return lines;
        }

        /// <summary>Screen and GUI scale as the player sees them right now.</summary>
        public static string Display() =>
            "Display: " + Read(() => Screen.width + "x" + Screen.height
                                     + " @" + Screen.currentResolution.refreshRateRatio.value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "Hz"
                                     + ", fullscreen=" + Screen.fullScreen)
            + ", GUI scale " + Read(() => Convert.ToString(AccessTools.Field(typeof(GuiScaler), "m_largeGuiScale")?.GetValue(null), System.Globalization.CultureInfo.InvariantCulture));

        private static string Read(Func<string> read)
        {
            try
            {
                return read() ?? "(null)";
            }
            catch (Exception e)
            {
                return "(unavailable: " + e.GetType().Name + ")";
            }
        }
    }
}
