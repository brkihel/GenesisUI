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
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_dropPrefab", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.GameObject")]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.PreviewStage), typeof(GenesisUI.Foundation.GenesisLog))]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_variant", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_quality", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    [GameContract("assembly_valheim", "ItemDrop+ItemData", "m_customData", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Collections.Generic.Dictionary\u00602[[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]")]
    internal sealed class ItemPreview
    {
        private const float Spin = 22f; // degrees per second

        private readonly PreviewStage _stage;
        private GameObject _holder;
        private GameObject _shownPrefab;
        private int _generation;
        private int _variant = -1, _quality = -1;
        private ulong _customData;

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
            if (!_stage.PrepareModel()) { Hide(); return false; }
            var prefab = item != null ? item.m_dropPrefab : null;
            if (prefab == null) { Hide(); return false; }
            // The stage rebuilt its scene, or the model was destroyed under us: build it again.
            if (_shownPrefab != null && (_holder == null || _generation != _stage.Generation))
            {
                GenesisLog.Info("Preview", "item model lost (" + (_holder == null ? "destroyed" : "stage rebuilt") + "): rebuilt");
                _generation = _stage.Generation;
                Clear();
            }
            ulong customData = GenesisUI.Data.TextMapFingerprint.Of(item.m_customData);
            if (prefab != _shownPrefab || _variant != item.m_variant || _quality != item.m_quality || _customData != customData)
            {
                Clear();
                Build(prefab);
                if (_holder == null) { Hide(); return false; }
                _shownPrefab = prefab;
                _variant = item.m_variant; _quality = item.m_quality; _customData = customData;
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
            if (_holder != null) { _holder.SetActive(false); Object.Destroy(_holder); }
            _holder = null;
            _shownPrefab = null;
        }
    }

    /// <summary>A visual snapshot of the player's live equipment, sanitized while inactive.
    /// No VisEquipment or foreign behaviour remains to instantiate attachments after activation.</summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Widgets.PreviewStage))]
    [GameContract("assembly_valheim", "VisEquipment", "m_skinColor", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Vector3")]
    [GameContract("assembly_valheim", "VisEquipment", "m_hairColor", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "UnityEngine.Vector3")]
    [GameContract("assembly_valheim", "VisEquipment", "m_modelIndex", Kind = GenesisUI.Foundation.Contracts.ContractMemberKind.Field, Static = GenesisUI.Foundation.Contracts.ContractStatic.Instance, ValueType = "System.Int32")]
    internal sealed class CharacterPreview
    {
        private const float CopyEvery = 0.25f;
        private readonly PreviewStage _stage;
        private readonly System.Collections.Generic.List<Renderer> _renderers = new System.Collections.Generic.List<Renderer>(64);
        private GameObject _holder;
        private Player _player;
        private int _fingerprint, _generation = -1;
        private float _copyIn;
        private AccessTools.FieldRef<VisEquipment, Vector3> _skin, _hair;
        private AccessTools.FieldRef<VisEquipment, int> _model;
        private CharacterPreview(PreviewStage stage) { _stage = stage; }
        public static CharacterPreview Create(RectTransform area)
        {
            var stage = PreviewStage.Create(area, "character");
            if (stage == null) return null;
            stage.Sway = 22f; stage.BaseYaw = -12f;
            return new CharacterPreview(stage)
            {
                _skin = AccessTools.FieldRefAccess<VisEquipment, Vector3>("m_skinColor"),
                _hair = AccessTools.FieldRefAccess<VisEquipment, Vector3>("m_hairColor"),
                _model = AccessTools.FieldRefAccess<VisEquipment, int>("m_modelIndex"),
            };
        }
        public void Show(Player player, float deltaSeconds)
        {
            if (player == null || !_stage.PrepareModel()) { Hide(); return; }
            _copyIn -= deltaSeconds;
            if (_holder == null || _generation != _stage.Generation || _player != player || _copyIn <= 0f)
            {
                _copyIn = CopyEvery;
                player.GetComponentsInChildren(true, _renderers);
                int fingerprint = 17;
                var equipment = player.GetComponent<VisEquipment>();
                if (equipment != null) fingerprint = _skin(equipment).GetHashCode() ^ _hair(equipment).GetHashCode() ^ _model(equipment);
                unchecked
                {
                    foreach (var renderer in _renderers)
                    {
                        fingerprint = fingerprint * 31 + renderer.GetInstanceID();
                        fingerprint = fingerprint * 31 + (renderer.enabled && renderer.gameObject.activeInHierarchy ? 1 : 0);
                        fingerprint = fingerprint * 31 + (renderer.sharedMaterial != null ? renderer.sharedMaterial.GetInstanceID() : 0);
                        if (renderer is SkinnedMeshRenderer skin) fingerprint = fingerprint * 31 + (skin.sharedMesh != null ? skin.sharedMesh.GetInstanceID() : 0);
                    }
                }
                if (_holder == null || _player != player || _generation != _stage.Generation || _fingerprint != fingerprint)
                {
                    Destroy();
                    _player = player; _fingerprint = fingerprint; _generation = _stage.Generation;
                    _holder = new GameObject("Character");
                    _holder.SetActive(false);
                    _holder.transform.SetParent(_stage.Pivot, false);
                    var copy = Object.Instantiate(player.gameObject, _holder.transform, false);
                    copy.transform.localPosition = Vector3.zero;
                    copy.transform.localRotation = Quaternion.identity;
                    copy.transform.localScale = player.transform.localScale;
                    PreviewStage.Strip(copy);
                    PreviewStage.StillAnimators(copy);
                    _holder.SetActive(true);
                    var p = _stage.Pivot.position;
                    _stage.Frame(new Bounds(p + new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 2f, 0.7f)), 4f, 1f);
                    _stage.UsePathFor(copy);
                    _stage.Inspect(copy, "character visual snapshot");
                }
            }
            _stage.Show(true);
        }
        public void Hide() => _stage.Show(false);
        public void Destroy()
        {
            if (_holder != null) { _holder.SetActive(false); Object.Destroy(_holder); }
            _holder = null;
        }
    }
}
