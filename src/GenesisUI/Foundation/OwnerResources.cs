using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Foundation
{
    /// <summary>Own allocations and subscriptions, including resources created before Build fails.</summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard))]
    internal static class OwnerResources
    {
        private sealed class Resources
        {
            internal readonly List<UnityEngine.Object> Objects = new List<UnityEngine.Object>();
            internal readonly List<Action> Cleanup = new List<Action>();
        }
        private static readonly Dictionary<string, Resources> Owners = new Dictionary<string, Resources>(StringComparer.Ordinal);

        private static Resources For(string owner)
        {
            if (!Owners.TryGetValue(owner, out var resources)) Owners.Add(owner, resources = new Resources());
            return resources;
        }

        public static T Own<T>(T value) where T : UnityEngine.Object
        {
            string owner = Guard.CurrentOwner;
            if (value != null && !string.IsNullOrEmpty(owner)) For(owner).Objects.Add(value);
            return value;
        }

        public static void OnRelease(string owner, Action cleanup)
        {
            if (string.IsNullOrEmpty(owner) || cleanup == null) throw new ArgumentException("Owner and cleanup are required");
            For(owner).Cleanup.Add(cleanup);
        }

        public static void Release(string owner)
        {
            if (!Owners.TryGetValue(owner, out var resources)) return;
            Owners.Remove(owner); // callbacks cannot modify the collection being drained
            for (int i = resources.Cleanup.Count - 1; i >= 0; i--)
                Guard.Try(owner + " release subscription", resources.Cleanup[i]);
            for (int i = resources.Objects.Count - 1; i >= 0; i--)
            {
                var value = resources.Objects[i];
                if (value == null) continue;
                Guard.Try(owner + " release object", () =>
                {
                    if (value is GameObject go) go.SetActive(false); // our generated objects only
                    UnityEngine.Object.Destroy(value);
                });
            }
        }

        public static void ReleaseAll()
        {
            foreach (var owner in new List<string>(Owners.Keys)) Release(owner);
        }
    }
}
