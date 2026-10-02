using System;
using System.IO;
using GenesisUI.Widgets;
using UnityEditor;
using UnityEngine;

namespace GenesisUI.Verification
{
    // Synthetic meshes only. The fixture compiles the exact runtime snapshot helper.
    public static class VisualRigVerification
    {
        public static void Verify()
        {
            try
            {
                foreach (float rendererScale in new[] { 0.01f, 1f, 100f })
                    foreach (float armatureScale in new[] { 0.01f, 1f, 100f })
                        Check(rendererScale, armatureScale);
                Debug.Log("GenesisUI visual rig verification passed: 9 scale combinations; geometry, private meshes, bones, pose, blend shapes and disposal");
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        private static void Check(float rendererScale, float armatureScale)
        {
            var source = new GameObject("Synthetic character");
            var stage = new GameObject("Synthetic stage");
            var mesh = new Mesh();
            var original = new Mesh();
            var copied = new Mesh();
            var material = new Material(Shader.Find("Unlit/Color"));
            material.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            var cameraObject = new GameObject("Verification camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 1.5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.cullingMask = 1 << 29;
            var texture = new RenderTexture(256, 256, 24);
            camera.targetTexture = texture;
            var flat = new GameObject("Previous flattened bake"); flat.SetActive(false);
            VisualRigSnapshot picture = null;
            try
            {
                source.transform.position = new Vector3(11f, 5f, -7f);
                source.transform.rotation = Quaternion.Euler(0f, 23f, 0f);
                source.AddComponent<Animator>();
                source.AddComponent<BoxCollider>();
                var armature = Child(source.transform, "Armature", Vector3.one * armatureScale);
                var bone = Child(armature, "Cape bone", Vector3.one);
                var renderTransform = Child(source.transform, "Imported mesh", Vector3.one * rendererScale);
                renderTransform.localRotation = Quaternion.Euler(0f, 17f, 0f);
                var skin = renderTransform.gameObject.AddComponent<SkinnedMeshRenderer>();
                var vertices = new[] { new Vector3(-0.3f, 0f, 0f), new Vector3(0.3f, 0f, 0f), new Vector3(0.3f, 2f, 0f), new Vector3(-0.3f, 2f, 0f) };
                for (int i = 0; i < vertices.Length; i++) vertices[i] = renderTransform.InverseTransformPoint(source.transform.TransformPoint(vertices[i]));
                mesh.vertices = vertices;
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                var weights = new BoneWeight[vertices.Length];
                for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
                mesh.boneWeights = weights;
                mesh.bindposes = new[] { bone.worldToLocalMatrix * renderTransform.localToWorldMatrix };
                mesh.AddBlendShapeFrame("Synthetic detail", 100f, new Vector3[vertices.Length], new Vector3[vertices.Length], new Vector3[vertices.Length]);
                mesh.RecalculateBounds();
                skin.sharedMesh = mesh;
                skin.sharedMaterial = material;
                skin.gameObject.layer = 29;
                skin.bones = new[] { bone };
                skin.rootBone = armature;
                skin.updateWhenOffscreen = true;
                skin.SetBlendShapeWeight(0, 37f);
                bone.localRotation = Quaternion.Euler(0f, 0f, 8f);
                picture = VisualRigSnapshot.Create(source.transform, new Renderer[] { skin }, stage.transform);
                picture.Root.gameObject.SetActive(true);
                var clone = picture.Root.GetComponentInChildren<SkinnedMeshRenderer>();
                Require(clone.sharedMesh != mesh, "Source mesh shared");
                Require(clone.bones[0] != bone && clone.bones[0].IsChildOf(picture.Root), "Source bone retained");
                Require(clone.rootBone.IsChildOf(picture.Root), "Source root bone retained");
                Require(clone.GetBlendShapeWeight(0) == 37f, "Blend shape lost");
                Require(picture.Root.GetComponentsInChildren<MonoBehaviour>(true).Length == 0, "Behaviour copied");
                Require(picture.Root.GetComponentsInChildren<Animator>(true).Length == 0 && picture.Root.GetComponentsInChildren<Collider>(true).Length == 0, "Animator or collider copied");
                skin.BakeMesh(original, false);
                clone.BakeMesh(copied, false);
                var a = original.vertices;
                var b = copied.vertices;
                float maxError = 0f;
                var oldBounds = new Bounds();
                var correctedBounds = new Bounds();
                for (int i = 0; i < a.Length; i++)
                {
                    var expected = Quaternion.Inverse(source.transform.rotation) * (skin.transform.TransformPoint(a[i]) - source.transform.position);
                    var actual = clone.transform.TransformPoint(b[i]);
                    maxError = Mathf.Max(maxError, Vector3.Distance(expected, actual));
                    if (i == 0) { oldBounds = new Bounds(expected, Vector3.zero); correctedBounds = new Bounds(actual, Vector3.zero); }
                    else { oldBounds.Encapsulate(expected); correctedBounds.Encapsulate(actual); }
                }
                Require(maxError < 0.002f, "Geometry differs: " + maxError);
                clone.gameObject.layer = 29;
                picture.Root.gameObject.SetActive(false);
                camera.transform.SetPositionAndRotation(source.transform.TransformPoint(new Vector3(0f, 1f, 5f)), source.transform.rotation * Quaternion.Euler(0f, 180f, 0f));
                var sourcePixels = Probe(camera, texture);
                source.SetActive(false); picture.Root.gameObject.SetActive(true);
                camera.transform.SetPositionAndRotation(new Vector3(0f, 1f, 5f), Quaternion.Euler(0f, 180f, 0f));
                var picturePixels = Probe(camera, texture);
                Require(sourcePixels.height > 150 && sourcePixels.height < 200, "Native skin is not approximately two metres");
                Require(sourcePixels.height == picturePixels.height && Math.Abs(sourcePixels.lit - picturePixels.lit) < 5, "GPU silhouette differs");
                picture.Root.gameObject.SetActive(false);
                flat.transform.SetPositionAndRotation(Quaternion.Inverse(source.transform.rotation) * (renderTransform.position - source.transform.position), Quaternion.Inverse(source.transform.rotation) * renderTransform.rotation);
                flat.transform.localScale = renderTransform.lossyScale;
                flat.AddComponent<MeshFilter>().sharedMesh = original;
                flat.AddComponent<MeshRenderer>().sharedMaterial = material;
                flat.layer = 29; flat.SetActive(true);
                var flatPixels = Probe(camera, texture);
                if (rendererScale != 1f) Require(flatPixels.height != sourcePixels.height, "Previous scale defect was not reproduced");
                Debug.Log(FormattableString.Invariant($"GenesisUI visual rig scales renderer={rendererScale} armature={armatureScale}: error={maxError:F6}, native/snapshot pixels={sourcePixels.height}/{picturePixels.height}, previous={flatPixels.height}, old bounds height={oldBounds.size.y:F3}"));
                if (rendererScale == 100f && armatureScale == 100f)
                {
                    var contact = new Texture2D(768, 256, TextureFormat.RGBA32, false);
                    contact.SetPixels(0, 0, 256, 256, sourcePixels.pixels);
                    contact.SetPixels(256, 0, 256, 256, picturePixels.pixels);
                    contact.SetPixels(512, 0, 256, 256, flatPixels.pixels);
                    File.WriteAllBytes(Path.GetFullPath("visual-rig-comparison.png"), contact.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(contact);
                }
                var owned = clone.sharedMesh;
                var pose = clone.bones[0].localRotation;
                bone.localRotation = Quaternion.identity;
                Require(clone.bones[0].localRotation == pose, "Source pose modifies snapshot");
                clone.sharedMesh.name = "Changed private mesh";
                Require(mesh.name != "Changed private mesh", "Snapshot modifies source mesh");
                picture.Dispose(); picture = null;
                Require(owned == null && stage.transform.childCount == 0, "Snapshot disposal leaked");
                Require(mesh != null && bone != null, "Source destroyed");
                source.SetActive(true);
                skin.rootBone = stage.transform; // Foreign transform must never survive remapping.
                bool rejected = false;
                try { picture = VisualRigSnapshot.Create(source.transform, new Renderer[] { skin }, stage.transform); }
                catch (InvalidOperationException) { rejected = true; }
                Require(rejected && stage.transform.childCount == 0, "External bone failure leaked hierarchy");
            }
            finally
            {
                if (picture != null) picture.Dispose();
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(stage);
                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(original);
                UnityEngine.Object.DestroyImmediate(copied);
                UnityEngine.Object.DestroyImmediate(flat);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        private static Transform Child(Transform parent, string name, Vector3 scale)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localScale = scale;
            return child;
        }
        private static (int height, int lit, Color[] pixels) Probe(Camera camera, RenderTexture texture)
        {
            camera.Render();
            var previous = RenderTexture.active;
            var readback = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = texture;
                readback.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); readback.Apply();
                var pixels = readback.GetPixels();
                int min = 256, max = -1, count = 0;
                for (int i = 0; i < pixels.Length; i++) if (pixels[i].r > 0.1f) { min = Math.Min(min, i / 256); max = Math.Max(max, i / 256); count++; }
                return (max - min + 1, count, pixels);
            }
            finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(readback); }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
