using GenesisUI.Foundation;
using GenesisUI.Foundation.Contracts;
using HarmonyLib;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>
    /// The item in the details panel as a slowly turning 3D model (D-034): the visual part of the
    /// item's own drop prefab (what vanilla's item stands show, <c>ItemStand.GetAttachPrefab</c>),
    /// copied onto a <see cref="PreviewStage"/> and stripped of everything that is not a picture.
    /// </summary>
    [GameContract("assembly_valheim", "ItemStand", "GetAttachPrefab")]
    [GameContract("assembly_valheim", "ItemStand", "GetAttachGameObject")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_dropPrefab")]
    internal sealed class ItemPreview
    {
        private const float Spin = 22f; // degrees per second

        private readonly PreviewStage _stage;
        private GameObject _holder;
        private GameObject _shownPrefab;
        private int _generation;

        private ItemPreview(PreviewStage stage) { _stage = stage; _generation = stage.Generation; }

        /// <summary>Null when 3D previews are not possible here (the caller keeps the 2D icon).</summary>
        public static ItemPreview Create(RectTransform area)
        {
            var stage = PreviewStage.Create(area, "item");
            if (stage == null) return null;
            stage.Spin = Spin;
            return new ItemPreview(stage);
        }

        /// <summary>Shows <paramref name="item"/> in 3D; false when it has no model (the caller shows the icon).</summary>
        public bool Show(ItemDrop.ItemData item)
        {
            var prefab = item != null ? item.m_dropPrefab : null;
            if (prefab == null) { Hide(); return false; }
            // The stage rebuilt its scene, or the model was destroyed under us: build it again.
            if (_shownPrefab != null && (_holder == null || _generation != _stage.Generation))
            {
                GenesisLog.Info("Preview", "item model lost (" + (_holder == null ? "destroyed" : "stage rebuilt") + "): rebuilt");
                _generation = _stage.Generation;
                Clear();
            }
            if (prefab != _shownPrefab)
            {
                Clear();
                if (!Guard.Try("item preview " + prefab.name, () => Build(prefab)) || _holder == null) { Hide(); return false; }
                _shownPrefab = prefab;
            }
            _stage.Show(true);
            return true;
        }

        public void Hide() => _stage.Show(false);

        private void Build(GameObject prefab)
        {
            var attach = ItemStand.GetAttachPrefab(prefab);
            var source = attach != null ? ItemStand.GetAttachGameObject(attach) : prefab;
            // Under an inactive holder: the copy never wakes up before it is only a picture.
            _holder = new GameObject("Item");
            _holder.SetActive(false);
            _holder.transform.SetParent(_stage.Pivot, false);
            var copy = Object.Instantiate(source, _holder.transform, false);
            copy.transform.localPosition = Vector3.zero;
            PreviewStage.Strip(copy);
            _holder.SetActive(true);
            _stage.UsePathFor(copy);
            _stage.Inspect(copy, "item " + prefab.name);
            _stage.Pivot.localRotation = Quaternion.Euler(0f, 30f, 0f);
            if (!_stage.Bounds(out var bounds)) { Clear(); return; }
            // Centred on the pivot, so it turns in place; framed for any angle (the bounds' sphere).
            copy.transform.position -= bounds.center - _stage.Pivot.position;
            bounds.center = _stage.Pivot.position;
            float r = bounds.extents.magnitude;
            _stage.Frame(new Bounds(bounds.center, new Vector3(r, r, r) * 2f), pitch: 14f, margin: 0.75f);
        }

        private void Clear()
        {
            if (_holder != null) Object.Destroy(_holder);
            _holder = null;
            _shownPrefab = null;
        }
    }

    /// <summary>
    /// The player's character in the inventory's equipment panel (D-034): a visual copy of the player
    /// prefab — never woken as a character (no <c>Player</c>, physics, network or game logic: removed
    /// before it is activated, so the game's lists of characters and players never see it) — dressed
    /// by vanilla's own <c>VisEquipment</c> from the local player's equipment, lit by the stage's gold
    /// light from above and swaying slowly.
    /// </summary>
    [GameContract("assembly_valheim", "Game", "m_playerPrefab")]
    [GameContract("assembly_valheim", "ZNetView", "m_forceDisableInit")]
    [GameContract("assembly_valheim", "VisEquipment", "m_leftItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_rightItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_chestItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_legItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_helmetItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_shoulderItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_beardItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_hairItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_utilityItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_leftBackItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_rightBackItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_trinketItem")]
    [GameContract("assembly_valheim", "VisEquipment", "m_modelIndex")]
    [GameContract("assembly_valheim", "VisEquipment", "m_skinColor")]
    [GameContract("assembly_valheim", "VisEquipment", "m_hairColor")]
    internal sealed class CharacterPreview
    {
        private const float CopyEvery = 0.25f;

        // VisEquipment's own state (what its non-networked branch draws from), copied one to one.
        private static readonly string[] IntFields =
        {
            "m_leftItem", "m_rightItem", "m_chestItem", "m_legItem", "m_helmetItem", "m_shoulderItem",
            "m_beardItem", "m_hairItem", "m_utilityItem", "m_leftBackItem", "m_rightBackItem",
            "m_shoulderItemVariant", "m_leftItemVariant", "m_leftBackItemVariant",
            "m_rightItemQuality", "m_rightBackItemQuality", "m_shoulderItemQuality", "m_leftItemQuality",
            "m_leftBackItemQuality", "m_modelIndex",
        };

        private readonly PreviewStage _stage;
        private AccessTools.FieldRef<VisEquipment, int>[] _ints;
        private AccessTools.FieldRef<VisEquipment, string> _trinket;
        private AccessTools.FieldRef<VisEquipment, Vector3> _skin, _hair;
        private GameObject _holder;
        private VisEquipment _copy;
        private int _childCount = -1;
        private VisEquipment _source;
        private readonly System.Collections.Generic.List<Transform> _children = new System.Collections.Generic.List<Transform>(256);
        private float _copyIn;
        private bool _failed;
        private int _failures, _generation = -1;
        private const int MaxFailures = 3;

        private CharacterPreview(PreviewStage stage) { _stage = stage; }

        /// <summary>Null when 3D previews are not possible here (the equipment panel stays as it was).</summary>
        public static CharacterPreview Create(RectTransform area)
        {
            var stage = PreviewStage.Create(area, "character");
            if (stage == null) return null;
            stage.Sway = 22f;
            stage.BaseYaw = -12f;
            return new CharacterPreview(stage);
        }

        /// <summary>Every frame while the inventory shows: builds once, then follows the player's equipment.</summary>
        public void Show(Player player, float deltaSeconds)
        {
            if (_failed || player == null) { _stage.Show(false); return; }
            // Lost (the stage rebuilt its scene, or the copy was destroyed): build it again, never give up
            // silently; after a few failed attempts the panel stays empty and the log says why.
            if (_copy == null || _generation != _stage.Generation)
            {
                if (_generation >= 0) GenesisLog.Info("Preview", "character copy lost (" + (_copy == null ? "destroyed" : "stage rebuilt") + "): rebuilding");
                Destroy();
                _generation = _stage.Generation;
                _childCount = -1;
                _copyIn = 0f;
                if (!Guard.Try("character preview", Build) || _copy == null) { Fail("could not build the copy"); return; }
            }
            _copyIn -= deltaSeconds;
            if (_copyIn <= 0f)
            {
                _copyIn = CopyEvery;
                _source = player.GetComponent<VisEquipment>();
                if (!Guard.Try("character preview dress", Dress)) { Fail("could not dress the copy"); return; }
            }
            _failures = 0;
            _stage.Show(true);
        }

        private void Fail(string why)
        {
            _stage.Show(false);
            Destroy();
            if (++_failures < MaxFailures) return; // tried again next frame
            _failed = true;
            GenesisLog.Warn("Preview", "character preview off after " + MaxFailures + " attempts: " + why);
        }

        public void Hide() => _stage.Show(false);

        public void Destroy()
        {
            if (_holder != null) Object.Destroy(_holder);
            _holder = null;
            _copy = null;
        }

        private void Build()
        {
            var prefab = Game.instance != null ? Game.instance.m_playerPrefab : null;
            if (prefab == null) return;
            _ints = new AccessTools.FieldRef<VisEquipment, int>[IntFields.Length];
            for (int i = 0; i < IntFields.Length; i++) _ints[i] = AccessTools.FieldRefAccess<VisEquipment, int>(IntFields[i]);
            _trinket = AccessTools.FieldRefAccess<VisEquipment, string>("m_trinketItem");
            _skin = AccessTools.FieldRefAccess<VisEquipment, Vector3>("m_skinColor");
            _hair = AccessTools.FieldRefAccess<VisEquipment, Vector3>("m_hairColor");

            // An inactive holder: the copy's Awake never runs as a character. Everything but the picture
            // and VisEquipment goes before it wakes; its network view is kept only so VisEquipment finds
            // one, and removes itself on wake (vanilla's preview switch, as in the main menu).
            _holder = new GameObject("Character");
            _holder.SetActive(false);
            _holder.transform.SetParent(_stage.Pivot, false);
            var copy = Object.Instantiate(prefab, _holder.transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            PreviewStage.Strip(copy, c => c is VisEquipment || c is ZNetView);
            PreviewStage.StillAnimators(copy);
            _copy = copy.GetComponent<VisEquipment>();
            if (_copy == null) { Destroy(); return; }
            ZNetView.m_forceDisableInit = true;
            try { _holder.SetActive(true); }
            finally { ZNetView.m_forceDisableInit = false; }
            // A standing viking: about 1.85 m tall; framed once, so a raised weapon never moves the camera.
            var p = _stage.Pivot.position;
            _stage.Frame(new Bounds(p + new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 2.0f, 0.7f)), pitch: 4f, margin: 1.0f);
            _stage.UsePathFor(copy);
            _stage.Inspect(copy, "character");
            GenesisLog.Info("Preview", "character preview built (layer " + PreviewStage.Layer + ")");
        }

        private void Dress()
        {
            var source = _source;
            if (source == null || _copy == null) return;
            for (int i = 0; i < _ints.Length; i++)
            {
                int v = _ints[i](source);
                if (_ints[i](_copy) != v) _ints[i](_copy) = v;
            }
            var trinket = _trinket(source) ?? "";
            if (_trinket(_copy) != trinket) _trinket(_copy) = trinket;
            _skin(_copy) = _skin(source);
            _hair(_copy) = _hair(source);
            // Vanilla attaches new equipment on the default layer: bring it onto the stage's.
            _copy.transform.GetComponentsInChildren(true, _children);
            int count = _children.Count;
            if (count != _childCount)
            {
                _childCount = count;
                PreviewStage.Restage(_copy.gameObject);
                _stage.UsePathFor(_copy.gameObject);
            }
        }
    }
}
