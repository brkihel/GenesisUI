using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace GenesisUI
{
    /// <summary>
    /// Entry point. F1 scope: load safely, describe the session, write reports on
    /// request and, in diagnostics builds, show the watermark and keep an own log.
    /// No module and no Harmony patch exists yet.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    [GameContract("assembly_utils", "ZInput", "GetButtonDown", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "ShowMessage")]
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_guiutils", "Localization", "Localize")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string ReportOwner = "host:report";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> RedactReports;
        internal static ConfigEntry<KeyboardShortcut> DiagnosticsKey;

        private ButtonConfig _diagnosticsButton;
        private Harmony _harmony;
        private bool _ready;

        private void Awake()
        {
            // Client-only (docs/DECISIONS.md D-001). On a dedicated server there is no UI to draw.
            if (GUIManager.IsHeadless())
            {
                Logger.LogInfo(PluginInfo.Name + " is client-only; nothing to do on a dedicated server.");
                enabled = false;
                return;
            }

            LogFileSink file = null;
#if GENESIS_DIAGNOSTICS
            file = LogFileSink.TryOpen(Path.Combine(BepInEx.Paths.BepInExRootPath, PluginInfo.Name, "logs"), "genesisui");
#endif
            GenesisLog.Init(Logger, PluginInfo.Name, Build.MinimumLogSeverity, file);

            foreach (var line in SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString()))
                GenesisLog.Info("Host", line);
            GenesisLog.Info("Host", file != null ? "own log file: " + file.FilePath : "own log file: off in this build");

            Guard.Run("host:awake", () =>
            {
                BindConfig();

                var missing = ContractResolver.Missing(typeof(Plugin));
                if (missing.Count > 0)
                {
                    // The report hotkey needs these members; without them it stays off.
                    foreach (var m in missing) GenesisLog.Warn("Host", "diagnostics hotkey disabled, missing " + m);
                }
                else
                {
                    RegisterDiagnosticsKey();
                }

                // No patch class exists in F1. Patches will go through GuardedPatcher.Apply, one class at a time.
                _harmony = new Harmony(PluginInfo.Guid);

#if GENESIS_DIAGNOSTICS
                Diagnostics.Watermark.Install(Build.Channel, Build.FullVersion);
#endif
                _ready = true;
            });

            GenesisLog.Info("Host", _ready ? "ready" : "started with errors; see the lines above");
        }

        private void BindConfig()
        {
            Enabled = Config.Bind("General", "Enabled", true,
                "Liga a interface GenesisUI. Desligado, o jogo usa a interface original. " +
                "(Esta versão de teste ainda não tem módulos visuais.)");

            DiagnosticsKey = Config.Bind("Diagnostics", "DiagnosticsKey", new KeyboardShortcut(KeyCode.F8),
                "Tecla de diagnóstico. Nesta versão, grava um relatório em BepInEx/GenesisUI/reports " +
                "e copia o caminho do arquivo.");

            RedactReports = Config.Bind("Diagnostics", "RedactReports", true,
                "Esconde dados pessoais nos relatórios (nome do personagem, mundo, endereço do servidor, " +
                "IDs de plataforma, usuário do Windows). Deixe ligado ao enviar relatórios para alguém.");
        }

        private void RegisterDiagnosticsKey()
        {
            _diagnosticsButton = new ButtonConfig
            {
                Name = "GenesisUI_Diagnostics",
                ShortcutConfig = DiagnosticsKey,
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
            };
            InputManager.Instance.AddButton(PluginInfo.Guid, _diagnosticsButton);
        }

        private void Update()
        {
            if (!_ready || _diagnosticsButton == null || ZInput.instance == null) return;
            if (Guard.IsTripped(ReportOwner)) return;
            if (ZInput.GetButtonDown(_diagnosticsButton.Name))
                Guard.Run(ReportOwner, WriteReport);
        }

        private void WriteReport()
        {
            var header = SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString());
            string path = ReportWriter.Write(PluginInfo.Name, header, Config, RedactReports.Value);
            string token = path != null ? "$genesisui_report_saved" : "$genesisui_report_failed";
            string text = Localization.instance != null ? Localization.instance.Localize(token) : token;
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }

        private void OnDestroy()
        {
            GenesisLog.Info("Host", "shutting down");
            GenesisLog.Shutdown();
        }
    }
}
