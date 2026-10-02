using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Host
{
    /// <summary>
    /// A piece of GenesisUI that owns one or more vanilla regions and draws their
    /// replacement (docs/ARCHITECTURE.md §3). Game members it touches are declared with
    /// [GameContract] on the class; the host checks them before Build.
    /// Every call is made through the guard: a throw here returns the regions to vanilla.
    /// </summary>
    internal interface IUiModule
    {
        /// <summary>Stable id, e.g. "hud.vitals". Used in logs, config and the overlay.</summary>
        string Id { get; }

        /// <summary>Localization token for the overlay and settings, e.g. "$genesisui_module_vitals".</summary>
        string NameToken { get; }

        IReadOnlyList<string> Regions { get; }

        /// <summary>Refresh rate in Hz. The Scheduler never calls Refresh faster than this.</summary>
        float RefreshRate { get; }

        /// <summary>Create own objects under <see cref="ModuleContext.Root"/>. Veiling is done by the host.</summary>
        void Build(ModuleContext context);

        void Refresh(float deltaSeconds);

        /// <summary>Destroy own objects. Must tolerate objects already destroyed by a scene change.</summary>
        void Teardown();
    }

    /// <summary>
    /// A module that draws over a vanilla window: when it faults, the host first closes that vanilla
    /// window (the inventory, the store, the map...) so the player never lands in vanilla's look or a
    /// half-restored window; the module is rebuilt and the next opening is GenesisUI's again.
    /// </summary>
    internal interface IRecoverable
    {
        void CloseVanillaWindow();
    }

    internal interface IModulePrerequisites
    {
        string UnsupportedReason { get; }
    }

    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.OwnerResources))]
    internal sealed class ModuleContext
    {
        public RectTransform Root;
        public Theme.ThemeRuntime Theme;
        public string Owner;
        public int Generation;
        public void OnRelease(System.Action cleanup) => Foundation.OwnerResources.OnRelease(Owner, cleanup);
    }

    internal enum ModuleState
    {
        /// <summary>Turned off in config (or GenesisUI is off).</summary>
        Disabled,
        /// <summary>Enabled, waiting for the game HUD to exist.</summary>
        Waiting,
        /// <summary>A declared game member is missing in this game version.</summary>
        Unsupported,
        /// <summary>A region is owned by someone else (another mod, another module).</summary>
        Blocked,
        Active,
        Building,
        Recovering,
        /// <summary>Threw; switched off for the session, vanilla is back.</summary>
        Faulted,
    }
}
