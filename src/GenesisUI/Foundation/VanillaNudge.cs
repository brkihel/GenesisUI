using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Foundation
{
    internal sealed class NudgeHandle
    {
        public string Owner;
        public string Label;
        public RectTransform Target;
        public Vector2 Original;
        public Vector2 Offset;
        public int Fought;
    }

    /// <summary>
    /// Moves a vanilla UI object by an offset, reversibly (the sibling of VanillaVeil, D-007):
    /// the original anchored position is recorded, re-applied every LateUpdate if vanilla
    /// resets it, and restored exactly on release or when the owner faults. Used when a vanilla
    /// element should stay vanilla but get out of the way of ours (key hints vs. our hotbar).
    /// </summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard), typeof(GenesisUI.Foundation.Faults.FaultRegistry), typeof(GenesisUI.Foundation.GenesisLog), typeof(GenesisUI.Foundation.NudgeHandle), typeof(GenesisUI.Foundation.Faults.FaultRecord))]
    internal static class VanillaNudge
    {
        private static readonly List<NudgeHandle> Active = new List<NudgeHandle>();

        static VanillaNudge()
        {
            Guard.Faults.Tripped += record => RestoreAll(record.Owner);
        }

        public static IReadOnlyList<NudgeHandle> Handles => Active;

        public static NudgeHandle Apply(string owner, string label, RectTransform target, Vector2 offset)
        {
            if (target == null)
            {
                GenesisLog.Warn("Nudge", owner + ": vanilla object for " + label + " not found; nothing moved");
                return null;
            }
            foreach (var h in Active)
            {
                if (h.Target == target)
                {
                    GenesisLog.Warn("Nudge", owner + ": " + label + " is already moved by " + h.Owner);
                    return null;
                }
            }
            var handle = new NudgeHandle { Owner = owner, Label = label, Target = target, Original = target.anchoredPosition, Offset = offset };
            Active.Add(handle);
            target.anchoredPosition = handle.Original + offset;
            GenesisLog.Info("Nudge", owner + " moved " + label + " by " + offset);
            return handle;
        }

        public static int RestoreAll(string owner)
        {
            int n = 0;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var h = Active[i];
                if (h.Owner != owner) continue;
                Active.RemoveAt(i);
                if (h.Target != null) h.Target.anchoredPosition = h.Original;
                GenesisLog.Info("Nudge", owner + " restored " + h.Label);
                n++;
            }
            return n;
        }

        /// <summary>Called every LateUpdate. Allocation-free.</summary>
        public static void Enforce()
        {
            for (int i = Active.Count - 1; i >= 0; i = System.Math.Min(i - 1, Active.Count - 1))
                Guard.Run(Active[i].Owner, EnforceHandle, Active[i]);
        }
        private static readonly System.Action<NudgeHandle> EnforceHandle = h =>
        {
            if (h.Target == null) { Active.Remove(h); return; }
            if (h.Target.anchoredPosition == h.Original + h.Offset) return;
            h.Original = h.Target.anchoredPosition;
            h.Target.anchoredPosition = h.Original + h.Offset;
            if (++h.Fought == 1) GenesisLog.Info("Nudge", h.Label + ": vanilla repositioned it; keeping our offset on top");
        };
    }
}
