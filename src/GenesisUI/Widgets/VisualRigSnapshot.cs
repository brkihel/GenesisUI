using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Widgets
{
    /// <summary>Copies only transforms and mesh renderers, remapping every skin bone to our hierarchy.
    /// Imported renderer scale and bind poses remain in their original relationship. No source mesh
    /// can be modified by this picture, and no game/animation/cloth component is instantiated.</summary>
    internal sealed class VisualRigSnapshot : IDisposable
    {
        private const int MaxTransforms = 4096;
        private readonly Dictionary<Transform, Transform> _transforms = new Dictionary<Transform, Transform>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly HashSet<Renderer> _lowerLods = new HashSet<Renderer>();
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        public Transform Root { get; private set; }
        public int RendererCount { get; private set; }
        public int TransformCount => _transforms.Count;

        public static VisualRigSnapshot Create(Transform source, IReadOnlyList<Renderer> candidates, Transform parent)
        {
            if (source == null || parent == null) throw new ArgumentNullException(nameof(source));
            var picture = new VisualRigSnapshot();
            try { picture.Build(source, candidates, parent); return picture; }
            catch { picture.Dispose(); throw; }
        }

        private void Build(Transform source, IReadOnlyList<Renderer> candidates, Transform parent)
        {
            var holder = new GameObject("Character visual rig");
            holder.SetActive(false);
            Root = holder.transform;
            Root.SetParent(parent, false);
            Root.localScale = source.lossyScale;
            _transforms.Add(source, Root);
            CopyChildren(source, Root, 0);
            foreach (var group in source.GetComponentsInChildren<LODGroup>(true))
            {
                var lods = group.GetLODs();
                for (int i = 1; i < lods.Length; i++)
                    foreach (var renderer in lods[i].renderers) if (renderer != null) _lowerLods.Add(renderer);
                if (lods.Length > 0)
                    foreach (var renderer in lods[0].renderers) _lowerLods.Remove(renderer);
            }
            foreach (var renderer in candidates)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || _lowerLods.Contains(renderer)) continue;
                var target = Mapped(renderer.transform);
                Renderer copy;
                if (renderer is SkinnedMeshRenderer skin)
                {
                    if (skin.sharedMesh == null) continue;
                    var mesh = CopyMesh(skin.sharedMesh);
                    var bones = skin.bones;
                    var mapped = new Transform[bones.Length];
                    for (int i = 0; i < bones.Length; i++) mapped[i] = Mapped(bones[i]);
                    var result = target.gameObject.AddComponent<SkinnedMeshRenderer>();
                    result.sharedMesh = mesh;
                    result.bones = mapped;
                    result.rootBone = Mapped(skin.rootBone);
                    result.localBounds = skin.localBounds;
                    result.quality = skin.quality;
                    result.updateWhenOffscreen = true;
                    for (int i = 0; i < mesh.blendShapeCount; i++) result.SetBlendShapeWeight(i, skin.GetBlendShapeWeight(i));
                    copy = result;
                }
                else if (renderer is MeshRenderer)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null) continue;
                    target.gameObject.AddComponent<MeshFilter>().sharedMesh = CopyMesh(filter.sharedMesh);
                    copy = target.gameObject.AddComponent<MeshRenderer>();
                }
                else continue;
                var materials = renderer.sharedMaterials;
                copy.sharedMaterials = materials;
                renderer.GetPropertyBlock(_properties);
                copy.SetPropertyBlock(_properties);
                for (int i = 0; i < materials.Length; i++)
                {
                    _properties.Clear(); renderer.GetPropertyBlock(_properties, i);
                    if (!_properties.isEmpty) copy.SetPropertyBlock(_properties, i);
                }
                RendererCount++;
            }
            if (RendererCount == 0) throw new InvalidOperationException("Character has no supported visible mesh renderer");
        }

        private void CopyChildren(Transform source, Transform parent, int depth)
        {
            if (depth > 128) throw new InvalidOperationException("Character visual hierarchy exceeds depth budget");
            for (int i = 0; i < source.childCount; i++)
            {
                var child = source.GetChild(i);
                if (_transforms.Count >= MaxTransforms) throw new InvalidOperationException("Character visual hierarchy exceeds transform budget");
                var copy = new GameObject(child.name).transform;
                copy.SetParent(parent, false);
                copy.localPosition = child.localPosition;
                copy.localRotation = child.localRotation;
                copy.localScale = child.localScale;
                copy.gameObject.SetActive(child.gameObject.activeSelf);
                _transforms.Add(child, copy);
                CopyChildren(child, copy, depth + 1);
            }
        }

        private Transform Mapped(Transform source)
        {
            if (source == null) return null;
            if (_transforms.TryGetValue(source, out var copy)) return copy;
            throw new InvalidOperationException("Character skin references a transform outside the visual hierarchy: " + source.name);
        }

        private Mesh CopyMesh(Mesh source)
        {
            var mesh = UnityEngine.Object.Instantiate(source);
            _meshes.Add(mesh);
            mesh.name = "GenesisUI visual mesh";
            return mesh;
        }

        public void Dispose()
        {
            if (Root != null) { Root.gameObject.SetActive(false); Release(Root.gameObject); }
            Root = null;
            foreach (var mesh in _meshes) if (mesh != null) Release(mesh);
            _meshes.Clear(); _transforms.Clear(); _lowerLods.Clear();
        }

        private static void Release(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
