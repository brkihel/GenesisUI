using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Foundation
{
    /// <summary>One veiled vanilla object and exactly what it looked like before.</summary>
    internal sealed class VeilHandle
    {
        public string Owner;
        public string Label;
        public GameObject Target;
        public CanvasGroup Group;
        public bool AddedGroup;
        public float PreviousAlpha;
        public bool PreviousBlocksRaycasts;
        public bool PreviousInteractable;
        public int Fought;
        internal CanvasGroupLease Lease;

        public bool Alive => Target != null && Group != null;
    }

    /// <summary>
    /// Hides vanilla UI without destroying or deactivating it (docs/DECISIONS.md D-007):
    /// other mods keep finding and extending the vanilla objects, and the kill switch
    /// always has something to bring back.
    ///
    /// The veil is a CanvasGroup at alpha 0 with raycasts off. Vanilla animators may
    /// drive that same alpha, so <see cref="Enforce"/> re-applies it every LateUpdate
    /// (after animation, before rendering) and logs once per target when vanilla
    /// fought it, which tells us the region needs a different strategy.
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Foundation.Faults.FaultRegistry), typeof(GenesisUI.Foundation.VeilHandle), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Foundation.SharedCanvasGroups), typeof(GenesisUI.Foundation.CanvasGroupLease), typeof(GenesisUI.Foundation.Faults.FaultRecord))]
    internal static class VanillaVeil
    {
        private static readonly List<VeilHandle> Active = new List<VeilHandle>();

        /// <summary>True while the diagnostics "show vanilla" toggle is on: veils stay registered but transparent.</summary>
        public static bool Lifted { get; private set; }

        static VanillaVeil()
        {
            Guard.Faults.Tripped += record => RestoreAll(record.Owner);
        }

        public static IReadOnlyList<VeilHandle> Handles => Active;

        /// <summary>True if <paramref name="target"/> is already veiled (by anyone).</summary>
        public static bool IsVeiled(GameObject target)
        {
            if (target == null) return false;
            for (int i = 0; i < Active.Count; i++)
                if (Active[i].Target == target) return true;
            return false;
        }

        /// <param name="quiet">Skip the info line: for objects vanilla creates in numbers (creature plates).</param>
        /// <returns>The handle, or null if the target is missing or already veiled by someone else.</returns>
        public static VeilHandle Apply(string owner, string label, GameObject target, bool quiet = false)
        {
            if (target == null)
            {
                GenesisLog.Warn("Veil", owner + ": vanilla object for " + label + " not found; nothing veiled");
                return null;
            }
            foreach (var h in Active)
            {
                if (h.Target == target)
                {
                    GenesisLog.Warn("Veil", owner + ": " + label + " is already veiled by " + h.Owner);
                    return null;
                }
            }

            var group = target.GetComponent<CanvasGroup>();
            var handle = new VeilHandle
            {
                Owner = owner,
                Label = label,
                Target = target,
                Lease = SharedCanvasGroups.Acquire(owner, target),
                AddedGroup = group == null,
            };
            handle.Group = handle.Lease.Group;
            handle.PreviousAlpha = handle.Lease.State.Alpha;
            handle.PreviousBlocksRaycasts = handle.Lease.State.Raycasts;
            handle.PreviousInteractable = handle.Lease.State.Interactable;

            Active.Add(handle);
            Hide(handle);
            if (!quiet) GenesisLog.Info("Veil", owner + " veiled " + label + (handle.AddedGroup ? " (own CanvasGroup)" : " (vanilla CanvasGroup)"));
            return handle;
        }

        public static void Restore(VeilHandle handle)
        {
            Restore(handle, quiet: false);
        }

        private static void Restore(VeilHandle handle, bool quiet)
        {
            if (handle == null || !Active.Remove(handle)) return;
            handle.Lease.Release();
            if (!quiet) GenesisLog.Info("Veil", handle.Owner + " restored " + handle.Label);
        }

        public static int RestoreAll(string owner)
        {
            int n = 0;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].Owner != owner) continue;
                Restore(Active[i], quiet: true);
                n++;
            }
            if (n > 0) GenesisLog.Info("Veil", owner + " restored " + n + " vanilla object(s)");
            return n;
        }

        /// <summary>Diagnostics: show vanilla under GenesisUI without releasing anything.</summary>
        public static void SetLifted(bool lifted)
        {
            Lifted = lifted;
            foreach (var h in Active)
            {
                if (!h.Alive) continue;
                if (lifted)
                {
                    h.Lease.Hide(false);
                }
                else
                {
                    Hide(h);
                }
            }
        }

        /// <summary>Called every LateUpdate. Allocation-free.</summary>
        public static void Enforce()
        {
            for (int i = Active.Count - 1; i >= 0; i = System.Math.Min(i - 1, Active.Count - 1))
            {
                var h = Active[i];
                Guard.Run(h.Owner, EnforceHandle, h);
            }
        }
        private static readonly System.Action<VeilHandle> EnforceHandle = h =>
        {
            if (!h.Alive) { h.Lease.Release(); Active.Remove(h); return; }
            if (Lifted || (h.Group.alpha == 0f && !h.Group.blocksRaycasts && !h.Group.interactable)) return;
            if (++h.Fought == 1) GenesisLog.Warn("Veil", h.Label + ": vanilla changed hidden alpha/input flags; re-applying every frame");
            Hide(h);
        };

        private static void Hide(VeilHandle h)
        {
            h.Lease.Hide(true);
        }
    }
}
