using System.Collections.Generic;
using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using GenesisUI.Host;
using GenesisUI.Theme;
using GenesisUI.Widgets;
using UnityEngine;

namespace GenesisUI.Modules.Windows
{
    /// <summary>
    /// What every GenesisUI window tab shares (D-032): it shows while the shell shows its tab, is built
    /// lazily on vanilla's inventory canvas as a design board, dims the world, hides vanilla's inventory
    /// panels (restored exactly), fades with the shell and keeps vanilla hidden through the close
    /// animation. A rebuild after a fault starts from a clean state (R-054). Subclasses draw the window
    /// and refresh it; vanilla stays the engine behind them.
    /// </summary>
    [GameContract("assembly_valheim", "InventoryGui", "Hide")]
    [GameContract("assembly_valheim", "InventoryGui", "IsVisible")]
    [GameContract("assembly_valheim", "InventoryGui", "m_player")]
    [GameContract("assembly_valheim", "InventoryGui", "m_crafting")]
    [GameContract("assembly_valheim", "InventoryGui", "m_info")]
    [GameContract("assembly_valheim", "InventoryGui", "m_container")]
    internal abstract class WindowModuleBase : IUiModule, IRecoverable
    {
        /// <summary>IRecoverable: on a fault the windows close (vanilla's never shows in their place).</summary>
        public void CloseVanillaWindow()
        {
            var gui = InventoryGui.instance;
            if (gui != null && InventoryGui.IsVisible()) gui.Hide();
        }

        protected const float PanelsTop = 102f, PanelsHeight = 673f;
        private const float CloseHoldSeconds = 0.6f;
        private static readonly string[] NoRegions = new string[0];
        private static readonly HashSet<WindowShellModule.Tab> Handled = new HashSet<WindowShellModule.Tab>();

        private RectTransform _root;
        private CanvasGroup _fade;
        private bool _applied;
        private float _closedFor;

        protected ThemeRuntime Theme { get; private set; }
        protected WindowParts Parts { get; private set; }
        /// <summary>The design board (WindowCanvas.Design) the window is laid out on.</summary>
        protected RectTransform Board { get; private set; }
        /// <summary>The strip between the tab bar and the hint bar, where panels go (y grows downwards).</summary>
        protected RectTransform Panels { get; private set; }

        public abstract string Id { get; }
        public abstract string NameToken { get; }
        public IReadOnlyList<string> Regions => NoRegions;
        public float RefreshRate => 0f;

        /// <summary>The shell tab this window draws.</summary>
        protected abstract WindowShellModule.Tab Tab { get; }

        protected string Owner => "module:" + Id;

        /// <summary>Whether a GenesisUI window draws this tab (the shell then leaves vanilla's dialog closed).</summary>
        internal static bool Handles(WindowShellModule.Tab tab) => Handled.Contains(tab);

        public void Build(ModuleContext context)
        {
            Theme = context.Theme;
            Parts = new WindowParts(Theme);
            _applied = false;
            _closedFor = 0f;
            Resolve();
            Handled.Add(Tab);
        }

        public void Refresh(float deltaSeconds)
        {
            var gui = InventoryGui.instance;
            var player = Player.m_localPlayer;
            bool on = gui != null && player != null && WindowShellModule.Showing && WindowShellModule.ActiveTab == Tab;
            if (!on)
            {
                if (_applied)
                {
                    if (gui == null || player == null || InventoryGui.IsVisible() || WindowShellModule.Showing) Unapply();
                    else
                    {
                        _closedFor += deltaSeconds;
                        SetFade(WindowShellModule.Opacity, false);
                        if (_closedFor >= CloseHoldSeconds && WindowShellModule.Opacity <= 0.001f) Unapply();
                    }
                }
                return;
            }
            _closedFor = 0f;
            if (!EnsureBuilt(gui)) return;
            if (!_applied) Apply(gui, player);
            WindowCanvas.Fit(Board);
            SetFade(WindowShellModule.Opacity, true);
            Tick(gui, player, deltaSeconds);
        }

        public void Teardown()
        {
            Handled.Remove(Tab);
            if (_applied) Unapply();
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;
            Board = Panels = null;
            Forget();
        }

        /// <summary>Resolve reflection here (after the host checked contracts), never in a constructor.</summary>
        protected virtual void Resolve() { }

        /// <summary>Build the window's objects under <see cref="Panels"/>.</summary>
        protected abstract void Draw();

        /// <summary>The window opened: read what only changes between openings.</summary>
        protected virtual void Opened(InventoryGui gui, Player player) { }

        /// <summary>Every frame while shown.</summary>
        protected abstract void Tick(InventoryGui gui, Player player, float deltaSeconds);

        /// <summary>The window closed (restore anything borrowed, release input).</summary>
        protected virtual void Closed() { }

        /// <summary>Drop every reference to built objects and cached state (the window is rebuilt from scratch).</summary>
        protected abstract void Forget();

        private bool EnsureBuilt(InventoryGui gui)
        {
            if (_root != null) return true;
            _root = WindowCanvas.CreateRoot(gui, "GenesisUI." + Id, behind: false);
            if (_root == null) return false;
            _fade = _root.gameObject.AddComponent<CanvasGroup>();
            _fade.alpha = 0f;
            Ui.Image(Ui.Fill(Ui.Child(_root, "Dim")), null, new Color(0f, 0f, 0f, 0.35f));
            Board = WindowCanvas.Area(_root, "Board");
            Panels = WindowCanvas.At(Board, "Panels", 0f, PanelsTop, WindowCanvas.Design.x, PanelsHeight);
            Draw();
            _root.gameObject.SetActive(false);
            return true;
        }

        private void Apply(InventoryGui gui, Player player)
        {
            _applied = true;
            _root.gameObject.SetActive(true);
            VanillaPanels.Hold(Id, gui);
            Opened(gui, player);
            GenesisLog.Info(Owner, "window shown; vanilla panels hidden (" + VanillaPanels.HolderCount + " window(s) holding them)");
        }

        private void Unapply()
        {
            _applied = false;
            VanillaPanels.Release(Id);
            _closedFor = 0f;
            Guard.Try(Owner + " closed", Closed);
            if (_root != null) _root.gameObject.SetActive(false);
        }

        private void SetFade(float alpha, bool interactive)
        {
            if (_fade == null) return;
            if (!Mathf.Approximately(_fade.alpha, alpha)) _fade.alpha = alpha;
            _fade.blocksRaycasts = interactive;
            _fade.interactable = interactive;
        }

        protected static string Localize(string text) => WindowParts.Localize(text);
    }
}
