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
    [BepInDependency(Adapters.Backpacks.BackpacksAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Adapters.Jewelcrafting.JewelcraftingAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Adapters.HipLantern.HipLanternAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(Adapters.AdventureBackpacks.AdventureBackpacksAdapter.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    [GameContract("assembly_utils", "ZInput", "GetButtonDown", Parameters = new[] { "System.String" })]
    [GameContract("assembly_utils", "ZInput", "instance")]
    [GameContract("assembly_valheim", "MessageHud", "ShowMessage")]
    [GameContract("assembly_valheim", "MessageHud", "instance")]
    [GameContract("assembly_valheim", "Hud", "m_rootObject", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    [GameContract("assembly_guiutils", "Localization", "Localize")]
    [GenesisUI.Foundation.Contracts.GameContract("assembly_guiutils", "Localization", "get_instance", Parameters = new string[] {  }, Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Method, Static = GenesisUI.Foundation.Contracts.ContractStatic.Static, ValueType = "Localization")]
    [DefaultExecutionOrder(30000)]
    public sealed class Plugin : BaseUnityPlugin
    {
        private const string KeyOwner = "host:diagnostics-key";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> RedactReports;
        internal static ConfigEntry<KeyboardShortcut> DiagnosticsKey;

        private ButtonConfig _diagnosticsButton;
        private DiagnosticsButtonRegistration _buttonRegistration;
        private Harmony _harmony;
        private ThemeRuntime _theme;
        private bool _ready;
        private static string _lastDisplay;
        private readonly List<System.Action> _unsubscribe = new List<System.Action>();
        private System.Action _guiAvailable;
        private bool _shutdown;
        private System.Action _readKey;
        private readonly Foundation.Logging.RecentSamples _frameSamples = new Foundation.Logging.RecentSamples();

        private void Awake()
        {
            _readKey = ReadDiagnosticsKey;
            LogFileSink file = null;
#if GENESIS_DIAGNOSTICS
            file = LogFileSink.TryOpen(Path.Combine(BepInEx.Paths.BepInExRootPath, PluginInfo.Name, "logs"), "genesisui");
#endif
            GenesisLog.Init(Logger, PluginInfo.Name, Build.MinimumLogSeverity, file);
            // D-031/D-040: config authority exists on a server; visual modules/patches do not.
            if (!Guard.Run("host:config", () =>
            {
                var missing = ContractResolver.Missing(typeof(Gameplay.InventorySettings));
                if (missing.Count > 0) throw new System.InvalidOperationException("Inventory config contracts missing: " + string.Join("; ", missing));
                Gameplay.InventorySettings.Bind(Config);
            })) { enabled = false; return; }
            if (GUIManager.IsHeadless())
            {
                Logger.LogInfo(PluginInfo.Name + " initialized inventory configuration sync; visual modules and gameplay patches stay off on a dedicated server.");
                enabled = false;
                return;
            }

            foreach (var line in SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString(), includeDisplay: false))
                GenesisLog.Info("Host", line);
            GenesisLog.Info("Host", file != null ? "own log file: " + file.FilePath : "own log file: off in this build");

            Guard.Run("host:awake", () =>
            {
                BindConfig();
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
                _theme.MetalEnabled = Config.Bind("Theme", "MetalShader", true,
                    "Molduras com o shader de metal (luz, relevo e brilho que passa). Desligado, as molduras usam a versão pintada, sem movimento. Vale ao reiniciar o jogo.").Value;
                _theme.BlurEnabled = Config.Bind("Theme", "MenuBlur", true,
                    "Fundo desfocado atrás do menu Esc. Se a tela ficar preta nesse menu, desligue: o fundo fica só escurecido.").Value;
                _theme.LightsEnabled = Config.Bind("Theme", "LightEffects", true,
                    "Efeitos de luz nas janelas: o feixe sobre as abas, a luz que percorre a moldura ao abrir e o brilho da receita que acabou de ficar pronta. Vale ao reiniciar o jogo.").Value;
                _theme.ModelsEnabled = Config.Bind("Theme", "Models3D", true,
                    "Personagem e item em 3D no inventário (o personagem com o seu equipamento, o item girando nos detalhes). Desligado, volta o ícone. Vale ao reiniciar o jogo.").Value;
                BindSound();
                Gameplay.SlotHotkeys.Bind(Config);
                Modules.Windows.WindowShellModule.ParallaxEnabled = Config.Bind("Windows", "Parallax", true,
                    "As janelas abertas se deslocam de leve contra o mouse e a luz do dourado se inclina para ele, dando profundidade.").Value;
                ModuleHost.Init(_theme, Enabled.Value);
                Watch(Enabled, (_, __) => Guard.Try("master toggle", () => ModuleHost.SetMasterEnabled(Enabled.Value)));

                ModuleHost.Register(new VitalsModule(Config), Config.Bind("Modules", "Vitals", true,
                    "Barras verticais de vida, vigor e eitr no canto inferior esquerdo. Desligado, o jogo mostra as barras originais."));
                ModuleHost.Register(new FoodModule(), Config.Bind("Modules", "Food", true,
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
                ModuleHost.Register(new Adapters.Backpacks.BackpacksAdapter(), Config.Bind("Modules", "Backpacks", true,
                    "Integra a mochila do Backpacks com os espaços de equipamento e mostra seu conteúdo abaixo do inventário. Exige uma versão verificada do mod."));
                ModuleHost.Register(new Adapters.AdventureBackpacks.AdventureBackpacksAdapter(), Config.Bind("Modules", "AdventureBackpacks", true,
                    "Integra a mochila equipada e a abertura nativa do Adventure Backpacks, quando disponível na versão suportada."));
                ModuleHost.Register(new Adapters.Jewelcrafting.JewelcraftingAdapter(), Config.Bind("Modules", "Jewelcrafting", true,
                    "Integra anel, colar, a aba de sockets da mesa de lapidação e os espaços de gemas, mantendo as ações e restrições do Jewelcrafting."));
                ModuleHost.Register(new Adapters.HipLantern.HipLanternAdapter(), Config.Bind("Modules", "HipLantern", true,
                    "Mostra o espaço independente da lanterna do HipLantern no equipamento. O mod continua controlando luz, combustível e uso."));
                ModuleHost.Register(new Gameplay.InventoryModule(), Config.Bind("Modules", "Inventory", true,
                    "Espaços do GenesisUI no inventário: tamanho definido pelo admin (32/40/48), consumo rápido, utilitários e equipamento. " +
                    "Desligado, o inventário volta ao do jogo; itens nos espaços especiais aparecem nas linhas de baixo."));
                ModuleHost.Register(new Modules.Windows.WindowShellModule(Config), Config.Bind("Modules", "Windows", true,
                    "Moldura das janelas: barra de abas em cima (Inventário, Habilidades, Mapa, Criação, Conquistas, Configurações) e dicas de atalho embaixo, com o inventário aberto. Desligado, as janelas ficam como no jogo."));
                ModuleHost.Register(new Modules.Windows.InventoryWindowModule(Config), Config.Bind("Modules", "InventoryWindow", true,
                    "Janela de inventário no layout do GenesisUI (inventário, equipamento, detalhes do item), por cima do inventário do jogo. Desligado, o inventário do jogo aparece como é."));
                ModuleHost.Register(new Modules.Windows.ItemTooltipModule(), Config.Bind("Modules", "ItemTooltips", true,
                    "Dicas dos itens com fontes e bordas discretas do GenesisUI. Preserva textos, gemas e dicas dos mods; desligado, o tooltip volta ao visual original."));
                ModuleHost.Register(new Modules.Windows.CraftingWindowModule(), Config.Bind("Modules", "CraftingWindow", true,
                    "Aba Criação no layout do concept: lista de receitas com busca e categorias, detalhes e materiais. Desligado, a aba mostra a criação original do jogo."));
                ModuleHost.Register(new Modules.Windows.SkillsWindowModule(), Config.Bind("Modules", "SkillsWindow", true,
                    "Aba Habilidades no layout do concept: personagem, habilidades, efeitos ativos e textos. Desligado, a aba abre o diálogo original do jogo."));
                ModuleHost.Register(new Modules.Windows.AchievementsWindowModule(), Config.Bind("Modules", "AchievementsWindow", true,
                    "Aba Conquistas: conquistas do jogo, troféus e detalhes. Desligado, a aba abre o painel original do jogo."));
                ModuleHost.Register(new Modules.Windows.SettingsWindowModule(Config), Config.Bind("Modules", "SettingsWindow", true,
                    "Aba Configurações: as opções do GenesisUI dentro do jogo, com explicação de cada uma."));
                ModuleHost.Register(new Modules.Build.BuildMenuModule(), Config.Bind("Modules", "BuildMenu", true,
                    "Menu de construção do martelo, enxada e cultivador no estilo do GenesisUI, com materiais e dicas. Desligado, o menu original do jogo aparece."));
                ModuleHost.Register(new Modules.Windows.StoreWindowModule(), Config.Bind("Modules", "StoreWindow", true,
                    "Loja dos comerciantes (Haldor, Hildir, Bruxa do Pântano) no estilo do GenesisUI. Desligado, a loja original do jogo aparece."));
                ModuleHost.Register(new Modules.Windows.DialogsModule(), Config.Bind("Modules", "Dialogs", true,
                    "Diálogos no estilo do GenesisUI: dividir pilha, estilo do item, pedras rúnicas e corvos, nomear placas e portais."));
                ModuleHost.Register(new Modules.Minimap.MapWindowModule(), Config.Bind("Modules", "MapWindow", true,
                    "Mapa grande com a moldura, filtros e marcadores do GenesisUI (o mapa em si continua o do jogo)."));
                ModuleHost.Register(new Modules.Minimap.MapMarkersModule(), Config.Bind("Modules", "MapMarkers", true,
                    "Marcadores do GenesisUI no mapa (masmorra, minério, covil, base...). Salvos como marcadores normais do jogo com uma etiqueta no nome."));
                ModuleHost.Register(new Modules.Windows.PauseMenuModule(), Config.Bind("Modules", "PauseMenu", true,
                    "Menu do Esc (pausa) no estilo do GenesisUI, com as mesmas opções do jogo."));
                ModuleHost.Register(new Modules.Hints.KeyHintsModule(Config), Config.Bind("Modules", "KeyHints", true,
                    "Dicas de atalho (Atacar, Bloquear, Construir...) com a fonte e as teclas do GenesisUI, menores."));
                ModuleHost.Register(new Modules.Slots.QuickSlotsModule(), Config.Bind("Modules", "Slots", true,
                    "Espaços de consumo rápido e de ação no HUD, acima à direita das comidas, com os seus atalhos (Configurações → Inventário → Atalhos)."));
                ModuleHost.Register(new Modules.Climate.ClimateModule(), Config.Bind("Modules", "Climate", true,
                    "A interface sente o mundo: geada, gotas, brasa e cinza nas molduras douradas conforme o clima; o ouro esfria à noite; batimento nas bordas com pouca vida; halo dourado quando descansado."));
                ModuleHost.Register(new SprintModule(Config), Config.Bind("Modules", "Sprint", true,
                    "Barra pequena de vigor acima dos itens: surge quando o vigor é gasto e some só depois de cheio. Desligado, ela não aparece."));

                // Jötunn raises this on every scene: fonts become available on the first one,
                // and the main scene brings the HUD the modules attach to.
                _guiAvailable = () => Guard.Run("host:gui", OnGuiAvailable);
                GUIManager.OnCustomGUIAvailable += _guiAvailable;

                // Patch classes one by one through the guarded patcher (PATCH-POLICY rule 5).
                _harmony = new Harmony(PluginInfo.Guid);
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryTabKeyPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.ShortcutInputPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventorySizePatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryTransitionPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryHoverPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.InventoryPlacementPatches));
                GuardedPatcher.Apply(_harmony, typeof(Patches.WalletLoadPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.EquipmentPatches));
                GuardedPatcher.Apply(_harmony, typeof(Patches.CraftingListPatch));
                GuardedPatcher.Apply(_harmony, typeof(Patches.RequirementBindingPatch));

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
            Watch(shared, changed);
            foreach (var e in panels.Values) Watch(e, changed);
        }

        /// <summary>The [Sound] section (Som in the settings window); every change applies at once (D-036).</summary>
        private void BindSound()
        {
            var enabled = Config.Bind("Sound", "Enabled", true,
                "Sons próprios do GenesisUI: um tilintar metálico baixo nas abas, ao abrir uma janela e ao terminar de criar um item. Seguem o volume do jogo.");
            var volume = Config.Bind("Sound", "Volume", 0.6f,
                new ConfigDescription("Volume dos sons do GenesisUI, sobre o volume do jogo.", new AcceptableValueRange<float>(0f, 1f)));
            Widgets.UiSound.Enabled = enabled.Value;
            Widgets.UiSound.Volume = volume.Value;
            Watch(enabled, (_, __) => Widgets.UiSound.Enabled = enabled.Value);
            Watch(volume, (_, __) => Widgets.UiSound.Volume = volume.Value);
            var cues = new[]
            {
                ("TabHover", "Tilintar baixo ao passar o mouse numa aba."),
                ("TabSelect", "Som ao trocar de aba (clique ou Q/E)."),
                ("WindowOpen", "Som ao abrir uma janela (inventário, mapa, criação...)."),
                ("CraftDone", "Duas batidas leves de martelo ao terminar de criar um item."),
            };
            for (int i = 0; i < cues.Length; i++)
            {
                int index = i;
                var entry = Config.Bind("Sound", cues[i].Item1, true, cues[i].Item2);
                Widgets.UiSound.CueEnabled[index] = entry.Value;
                Watch(entry, (_, __) => Widgets.UiSound.CueEnabled[index] = entry.Value);
            }
        }

        private void RegisterDiagnosticsKey()
        {
            var missing = ContractResolver.Missing(typeof(DiagnosticsButtonRegistration));
            if (missing.Count > 0) throw new System.InvalidOperationException("Diagnostic button contracts missing: " + string.Join("; ", missing));
            _diagnosticsButton = new ButtonConfig
            {
                Name = "GenesisUI_Diagnostics",
                ShortcutConfig = DiagnosticsKey,
                ActiveInGUI = true,
                ActiveInCustomGUI = true,
            };
            InputManager.Instance.AddButton(PluginInfo.Guid, _diagnosticsButton);
            _buttonRegistration = new DiagnosticsButtonRegistration(_diagnosticsButton);
        }

        private void Update()
        {
            if (!_ready) return;
#if GENESIS_DIAGNOSTICS
            if (!Guard.Run("diag:overlay", OverlayTick, Time.unscaledDeltaTime)) Guard.Try("close faulted diagnostics", Diagnostics.Overlay.Close);
#endif
            Guard.Run(KeyOwner, _readKey);
        }
#if GENESIS_DIAGNOSTICS
        private static readonly System.Action<float> OverlayTick = Diagnostics.Overlay.Tick;
#endif
        private void ReadDiagnosticsKey()
        {
            if (_diagnosticsButton == null || ZInput.instance == null || Guard.IsTripped(KeyOwner)) return;
            if (!ZInput.GetButtonDown(_diagnosticsButton.Name)) return;
#if GENESIS_DIAGNOSTICS
            Guard.Run(KeyOwner, Diagnostics.Overlay.Toggle);
#else
            Guard.Run(KeyOwner, () => ShowReportResult(WriteReport()));
#endif
        }

        /// <summary>
        /// Modules refresh after every Update: vanilla opens its windows in its own Update (Tab, a
        /// workbench, a chest, Esc, the hammer, M), and a module refreshed before that hid them one
        /// frame late, so vanilla flashed behind GenesisUI's window (R-059). Here they are hidden in
        /// the frame they open, before anything is drawn.
        /// </summary>
        private void LateUpdate()
        {
            if (!_ready) return;
            _frameSamples.Add(Time.unscaledDeltaTime * 1000.0);
            ModuleHost.Tick(Time.unscaledDeltaTime);
            ModuleHost.LateTick();
        }

        private string WriteReport()
        {
            var header = SessionHeader.Build(PluginInfo.Name, Build.FullVersion, Build.Channel.ToString(), includeDisplay: true);
            return ReportWriter.Write(PluginInfo.Name, header, Config, RedactReports.Value, ReportSections());
        }

        private IEnumerable<KeyValuePair<string, IEnumerable<string>>> ReportSections()
        {
            yield return new KeyValuePair<string, IEnumerable<string>>("Shader provenance", new[] { _theme != null ? _theme.ShaderEvidence : "theme unavailable" });
            yield return new KeyValuePair<string, IEnumerable<string>>("Preview stages", Widgets.PreviewStage.Diagnostics());
            yield return new KeyValuePair<string, IEnumerable<string>>("Recent frame intervals", new[] { "window samples " + _frameSamples.Count + "; p50/p95 ms " + _frameSamples.Percentile(0.5).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "/" + _frameSamples.Percentile(0.95).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "; includes game, mods, synchronization and UI; not isolated GenesisUI cost" });
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
            if (_shutdown) return;
            _shutdown = true; _ready = false;
            GenesisLog.Info("Host", "shutting down");
            if (_guiAvailable != null) GUIManager.OnCustomGUIAvailable -= _guiAvailable;
            foreach (var unsubscribe in _unsubscribe) Guard.Try("unsubscribe config", unsubscribe);
            _unsubscribe.Clear();
            Guard.Try("module shutdown", ModuleHost.Shutdown);
            Guard.Try("shared panels shutdown", Modules.Windows.VanillaPanels.Shutdown);
            Guard.Try("skin shutdown", VanillaSkin.RestoreEveryOwner);
            Guard.Try("shared groups shutdown", SharedCanvasGroups.ReleaseEveryOwner);
            Guard.Try("owned resources shutdown", OwnerResources.ReleaseAll);
            Guard.Try("fault popup shutdown", Diagnostics.FaultPopup.Shutdown);
#if GENESIS_DIAGNOSTICS
            Guard.Try("overlay shutdown", Diagnostics.Overlay.Shutdown);
            Guard.Try("watermark shutdown", Diagnostics.Watermark.Shutdown);
#endif
            Guard.Try("input shutdown", InputLeases.Shutdown);
            Guard.Try("audio shutdown", Widgets.UiSound.Shutdown);
            Guard.Try("hotkeys shutdown", Gameplay.SlotHotkeys.Shutdown);
            if (_buttonRegistration != null) Guard.Try("diagnostics button shutdown", _buttonRegistration.Dispose);
            _buttonRegistration = null;
            Guard.Try("window settings shutdown", Widgets.WindowCanvas.Shutdown);
            Guard.Try("inventory config shutdown", Gameplay.InventorySettings.Shutdown);
            Guard.Try("requirement bindings shutdown", Modules.Windows.RequirementBindings.Clear);
            if (_harmony != null) Guard.Try("own patch shutdown", () => GuardedPatcher.Shutdown(_harmony));
            if (_theme != null) Guard.Try("theme shutdown", _theme.Shutdown);
            _theme = null; _harmony = null; _guiAvailable = null; _diagnosticsButton = null;
            Enabled = null; RedactReports = null; DiagnosticsKey = null; _lastDisplay = null;
            GenesisLog.Shutdown();
        }
        private void Watch<T>(ConfigEntry<T> entry, System.EventHandler handler)
        {
            entry.SettingChanged += handler;
            _unsubscribe.Add(() => entry.SettingChanged -= handler);
        }
    }
}
