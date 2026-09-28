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

        /// <returns>The handle, or null if the target is missing or already veiled by someone else.</returns>
        public static VeilHandle Apply(string owner, string label, GameObject target)
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
                Group = group != null ? group : target.AddComponent<CanvasGroup>(),
                AddedGroup = group == null,
            };
            handle.PreviousAlpha = handle.Group.alpha;
            handle.PreviousBlocksRaycasts = handle.Group.blocksRaycasts;
            handle.PreviousInteractable = handle.Group.interactable;

            Active.Add(handle);
            Hide(handle);
            GenesisLog.Info("Veil", owner + " veiled " + label + (handle.AddedGroup ? " (own CanvasGroup)" : " (vanilla CanvasGroup)"));
            return handle;
        }

        public static void Restore(VeilHandle handle)
        {
            if (handle == null || !Active.Remove(handle)) return;
            if (!handle.Alive) return; // destroyed with its scene: nothing to give back

            if (handle.AddedGroup)
            {
                Object.Destroy(handle.Group);
            }
            else
            {
                handle.Group.alpha = handle.PreviousAlpha;
                handle.Group.blocksRaycasts = handle.PreviousBlocksRaycasts;
                handle.Group.interactable = handle.PreviousInteractable;
            }
            GenesisLog.Info("Veil", handle.Owner + " restored " + handle.Label);
        }

        public static int RestoreAll(string owner)
        {
            int n = 0;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].Owner != owner) continue;
                Restore(Active[i]);
                n++;
            }
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
                    h.Group.alpha = h.AddedGroup ? 1f : h.PreviousAlpha;
                    h.Group.blocksRaycasts = h.PreviousBlocksRaycasts;
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
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var h = Active[i];
                if (!h.Alive)
                {
                    Active.RemoveAt(i); // its scene is gone
                    continue;
                }
                if (Lifted || h.Group.alpha == 0f) continue;

                h.Fought++;
                if (h.Fought == 1)
                    GenesisLog.Warn("Veil", h.Label + ": vanilla changed the veiled alpha (an animator drives it); re-applying every frame");
                Hide(h);
            }
        }

        private static void Hide(VeilHandle h)
        {
            h.Group.alpha = 0f;
            h.Group.blocksRaycasts = false;
            h.Group.interactable = false;
        }
    }
}
