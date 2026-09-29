using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Modules.Boss;
using GenesisUI.Modules.Enemy;
using GenesisUI.Modules.Food;
using GenesisUI.Modules.Hover;
using GenesisUI.Modules.Notice;
using GenesisUI.Modules.Hotbar;
using GenesisUI.Modules.Minimap;
using GenesisUI.Modules.Sprint;
using GenesisUI.Modules.Status;
using GenesisUI.Modules.Vitals;
using GenesisUI.Theme;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace GenesisUI
{
    /// <summary>
    /// Entry point. Loads safely on clients only, describes the session, loads the theme
    /// on the first GUI, and hands the HUD to the ModuleHost. In diagnostics builds F8
    /// opens the diagnostics panel; in Release it writes the report.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    [GameContract("assembly_utils", "ZInput", "GetButtonDown", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "ShowMessage")]
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_valheim", "Hud", "m_rootObject")]
    [GameContract("assembly_guiutils", "Localization", "Localize")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string KeyOwner = "host:diagnostics-key";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> RedactReports;
        internal static ConfigEntry<KeyboardShortcut> DiagnosticsKey;

        private ButtonConfig _diagnosticsButton;
        private Harmony _harmony;
        private ThemeRuntime _theme;
        private bool _ready;
        private static string _lastDisplay;

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

            foreach (var line in SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString(), includeDisplay: false))
                GenesisLog.Info("Host", line);
            GenesisLog.Info("Host", file != null ? "own log file: " + file.FilePath : "own log file: off in this build");

            Guard.Run("host:awake", () =>
            {
                BindConfig();
                Gameplay.InventorySettings.Bind(Config);
                Widgets.WindowCanvas.Bind(Config);

                var missing = ContractResolver.Missing(typeof(Plugin));
                if (missing.Count > 0)
                {
                    foreach (var m in missing) GenesisLog.Warn("Host", "missing " + m + "; GenesisUI stays inactive");
                    return;
                }
                RegisterDiagnosticsKey();

                _theme = new ThemeRuntime(Path.GetDirectoryName(Info.Location));
                BindBackgrounds(_theme);
                ModuleHost.Init(_theme, Enabled.Value);
                Enabled.SettingChanged += (_, __) => Guard.Try("master toggle", () => ModuleHost.SetMasterEnabled(Enabled.Value));

                ModuleHost.Register(new VitalsModule(Config), Config.Bind("Modules", "Vitals", true,
                    "Barras verticais de vida, vigor e eitr no canto inferior esquerdo. Desligado, o jogo mostra as barras originais."));
                ModuleHost.Register(new FoodModule(Config), Config.Bind("Modules", "Food", true,
                    "Os três espaços de comida ao lado das barras, com o tempo restante. Desligado, o jogo mostra a comida original."));
                ModuleHost.Register(new HotbarModule(Config), Config.Bind("Modules", "Hotbar", true,
                    "Barra de itens (1 a 8) emoldurada, no centro de baixo. Desligado, o jogo mostra a barra original."));
                ModuleHost.Register(new MinimapModule(Config), Config.Bind("Modules", "Minimap", true,
                    "Minimapa redondo no canto superior direito, com vento, dia e hora em cima e o bioma embaixo. Desligado, o jogo mostra o minimapa original."));
                ModuleHost.Register(new BossModule(Config), Config.Bind("Modules", "Boss", true,
                    "Placa do chefe no topo, com nome, estrelas e vida. Desligado, o jogo mostra a barra de chefe original."));
                ModuleHost.Register(new EnemyModule(Config), Config.Bind("Modules", "Enemy", true,
                    "Placas das criaturas (nome, estrelas, vida e alerta) no visual do GenesisUI, onde o jogo mostra as dele. Desligado, o jogo mostra as originais."));
                ModuleHost.Register(new HoverModule(Config), Config.Bind("Modules", "Hover", true,
                    "Cartão de interação ao lado da mira (nome do objeto e ações). Desligado, o jogo mostra o texto original."));
                ModuleHost.Register(new NoticeModule(Config), Config.Bind("Modules", "Notice", true,
                    "Notificações do canto superior esquerdo e mensagens do centro no visual do GenesisUI. Desligado, o jogo mostra as originais."));
                ModuleHost.Register(new StatusModule(Config), Config.Bind("Modules", "Status", true,
                    "Efeitos ativos e o poder do guardião em quadros com nome e tempo. Desligado, o jogo mostra os efeitos originais."));
                ModuleHost.Register(new Gameplay.InventoryModule(), Config.Bind("Modules", "Inventory", true,
                    "Espaços do GenesisUI no inventário: tamanho definido pelo admin (32/40/48), consumo rápido, utilitários e equipamento. " +
                    "Desligado, o inventário volta ao do jogo; itens nos espaços especiais aparecem nas linhas de baixo."));
                ModuleHost.Register(new Modules.Windows.WindowShellModule(Config), Config.Bind("Modules", "Windows", true,
                    "Moldura das janelas: barra de abas em cima (Inventário, Habilidades, Mapa, Criação, Conquistas, Configurações) e dicas de atalho embaixo, com o inventário aberto. Desligado, as janelas ficam como no jogo."));
                ModuleHost.Register(new Modules.Windows.InventoryWindowModule(Config), Config.Bind("Modules", "InventoryWindow", true,
                    "Janela de inventário no layout do GenesisUI (inventário, equipamento, detalhes do item), por cima do inventário do jogo. Desligado, o inventário do jogo aparece como é."));
                ModuleHost.Register(new SprintModule(Config), Config.Bind("Modules", "Sprint", true,
                    "Barra pequena de vigor acima dos itens: surge quando o vigor é gasto e some só depois de cheio. Desligado, ela não aparece."));

                // Jötunn raises this on every scene: fonts become available on the first one,
                // and the main scene brings the HUD the modules attach to.
                GUIManager.OnCustomGUIAvailable += () => Guard.Try("gui available", OnGuiAvailable);

                // Patch classes one by one through the guarded patcher (PATCH-POLICY rule 5).
                _harmony = new Harmony(PluginInfo.Guid);
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryTabKeyPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventorySizePatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryPlacementPatches));
                GuardedPatcher.Apply(_harmony, typeof(Patches.EquipmentPatches));

#if GENESIS_DIAGNOSTICS
                Diagnostics.Watermark.Install(Build.Channel, Build.FullVersion);
                Diagnostics.Overlay.Init(_theme, WriteReport);
#endif
                _ready = true;
            });

            GenesisLog.Info("Host", _ready ? "ready" : "started with errors; see the lines above");
        }

        private void OnGuiAvailable()
        {
            LogDisplayIfChanged();
            _theme.EnsureLoaded();
#if GENESIS_DIAGNOSTICS
            Diagnostics.Overlay.OnGuiAvailable();
#endif
            ModuleHost.OnGuiAvailable();
        }

        private static void LogDisplayIfChanged()
        {
            string display = SessionHeader.Display();
            if (display == _lastDisplay) return;
            _lastDisplay = display;
            GenesisLog.Info("Host", display);
        }

        private void BindConfig()
        {
            Enabled = Config.Bind("General", "Enabled", true,
                "Liga a interface GenesisUI. Desligado, todas as partes voltam para a interface original do jogo, na hora.");

            DiagnosticsKey = Config.Bind("Diagnostics", "DiagnosticsKey", new KeyboardShortcut(KeyCode.F8),
                "Tecla de diagnóstico. Nas versões de teste abre o painel de diagnóstico; " +
                "na versão final grava um relatório em BepInEx/GenesisUI/reports e copia o caminho.");

            RedactReports = Config.Bind("Diagnostics", "RedactReports", true,
                "Esconde dados pessoais nos relatórios (nome do personagem, mundo, endereço do servidor, " +
                "IDs de plataforma, usuário do Windows). Deixe ligado ao enviar relatórios para alguém.");
        }

        /// <summary>
        /// [Backgrounds]: the panel material's opacity, one default and an override per panel
        /// (-1 = use the default). Only the material fades; frames, texts and icons do not.
        /// </summary>
        private void BindBackgrounds(ThemeRuntime theme)
        {
            var shared = Config.Bind("Backgrounds", "Default", 0.6f,
                new ConfigDescription("Opacidade do fundo de pedra escura dentro das molduras (0 = sem fundo, 1 = opaco). " +
                    "Só o fundo fica transparente; moldura, textos e ícones não.", new AcceptableValueRange<float>(0f, 1f)));
            var panels = new Dictionary<string, ConfigEntry<float>>();
            foreach (var kv in new[]
            {
                new KeyValuePair<string, string>("Vitals", "barras de vida, vigor e eitr (parte vazia) e o medalhão"),
                new KeyValuePair<string, string>("Food", "espaços de comida"),
                new KeyValuePair<string, string>("Hotbar", "barra de itens"),
                new KeyValuePair<string, string>("Minimap", "placas de dia/hora e de bioma do minimapa"),
                new KeyValuePair<string, string>("Boss", "placa do chefe"),
                new KeyValuePair<string, string>("Enemy", "placas das criaturas"),
                new KeyValuePair<string, string>("Hover", "cartão de interação"),
                new KeyValuePair<string, string>("Notice", "notificações"),
                new KeyValuePair<string, string>("Status", "quadros de efeitos"),
                new KeyValuePair<string, string>("Sprint", "barra de corrida"),
                new KeyValuePair<string, string>("Windows", "barras e painéis das janelas (inventário, criação...)"),
            })
            {
                panels[kv.Key] = Config.Bind("Backgrounds", kv.Key, -1f,
                    new ConfigDescription("Opacidade do fundo: " + kv.Value + ". -1 = usa o valor de Default.",
                        new AcceptableValueRange<float>(-1f, 1f)));
            }
            theme.BackgroundOpacity = panel =>
                panel != null && panels.TryGetValue(panel, out var e) && e.Value >= 0f ? e.Value : shared.Value;
            System.EventHandler changed = (_, __) => Guard.Try("background opacity", theme.RefreshBackgrounds);
            shared.SettingChanged += changed;
            foreach (var e in panels.Values) e.SettingChanged += changed;
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
            if (!_ready) return;
            ModuleHost.Tick(Time.unscaledDeltaTime);
#if GENESIS_DIAGNOSTICS
            Diagnostics.Overlay.Tick(Time.unscaledDeltaTime);
#endif
            if (_diagnosticsButton == null || ZInput.instance == null || Guard.IsTripped(KeyOwner)) return;
            if (!ZInput.GetButtonDown(_diagnosticsButton.Name)) return;
#if GENESIS_DIAGNOSTICS
            Guard.Run(KeyOwner, Diagnostics.Overlay.Toggle);
#else
            Guard.Run(KeyOwner, () => ShowReportResult(WriteReport()));
#endif
        }

        private void LateUpdate()
        {
            if (_ready) ModuleHost.LateTick();
        }

        private string WriteReport()
        {
            var header = SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString(), includeDisplay: true);
            return ReportWriter.Write(PluginInfo.Name, header, Config, RedactReports.Value, ReportSections());
        }

        private IEnumerable<KeyValuePair<string, IEnumerable<string>>> ReportSections()
        {
            yield return new KeyValuePair<string, IEnumerable<string>>("Modules", ModuleHost.Modules.Select(e =>
                e.Module.Id + ": " + e.State + (e.Reason == null ? "" : " — " + e.Reason)
                + (e.RefreshCount > 0 ? " (" + (e.RefreshSecondsTotal / e.RefreshCount * 1000.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " ms/refresh over " + e.RefreshCount + ")" : "")));
            yield return new KeyValuePair<string, IEnumerable<string>>("Regions", RegionRegistry.Snapshot().Select(r => r.Key + " -> " + r.Value));
            yield return new KeyValuePair<string, IEnumerable<string>>("Veils", VanillaVeil.Handles.Select(h =>
                h.Label + " (" + (h.AddedGroup ? "own" : "vanilla") + " CanvasGroup)" + (h.Fought > 0 ? " fought " + h.Fought + "x" : "")));
            yield return new KeyValuePair<string, IEnumerable<string>>("Vanilla health panel", HudDump.HealthPanel());
            yield return new KeyValuePair<string, IEnumerable<string>>("Theme", new[]
            {
                "fonts: " + (_theme?.FontCount ?? 0) + "/5, sprites: " + (_theme?.SpriteCount ?? 0),
            });
        }

        private static void ShowReportResult(string path)
        {
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
