using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace GenesisUI.Foundation
{
    internal enum PatchState
    {
        Applied,
        Skipped,
        Failed,
    }

    internal sealed class PatchResult
    {
        public string PatchClass;
        public PatchState State;
        public string Reason;
    }

    /// <summary>
    /// Applies Harmony patch classes one at a time (docs/PATCH-POLICY.md rule 5).
    /// A class whose [GameContract]s do not resolve is skipped; a class that throws
    /// while patching is rolled back and recorded as failed. Nothing else is affected.
    /// PatchAll over the assembly is never used: one missing target would take every
    /// patch down with it.
    /// </summary>
    internal static class GuardedPatcher
    {
        private static readonly object Lock = new object();
        private static readonly List<PatchResult> AllResults = new List<PatchResult>();

        public static IReadOnlyList<PatchResult> Results
        {
            get { lock (Lock) return AllResults.ToList(); }
        }

        public static PatchResult Apply(Harmony harmony, Type patchClass)
        {
            var result = new PatchResult { PatchClass = patchClass.FullName };
            string owner = "patch:" + patchClass.Name;

            var missing = ContractResolver.Missing(patchClass);
            if (missing.Count > 0)
            {
                result.State = PatchState.Skipped;
                result.Reason = "missing game members: " + string.Join("; ", missing);
                GenesisLog.Warn("Patch", patchClass.Name + " skipped, " + result.Reason);
            }
            else
            {
                try
                {
                    harmony.CreateClassProcessor(patchClass).Patch();
                    result.State = PatchState.Applied;
                    GenesisLog.Info("Patch", patchClass.Name + " applied");
                }
                catch (Exception e)
                {
                    RollBack(harmony, patchClass);
                    result.State = PatchState.Failed;
                    result.Reason = e.GetType().Name + ": " + e.Message;
                    Guard.Fault(owner, e);
                }
            }

            lock (Lock) AllResults.Add(result);
            return result;
        }

        /// <summary>
        /// Removes whatever this class managed to patch before it failed. Only patch
        /// methods declared by <paramref name="patchClass"/> under our own Harmony id
        /// are touched: another mod's patch is never removed (PATCH-POLICY rule 4).
        /// </summary>
        private static void RollBack(Harmony harmony, Type patchClass)
        {
            try
            {
                foreach (var original in harmony.GetPatchedMethods().ToList())
                {
                    var info = Harmony.GetPatchInfo(original);
                    if (info == null) continue;
                    var ours = info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
                        .Where(p => p.owner == harmony.Id && p.PatchMethod.DeclaringType == patchClass)
                        .ToList();
                    foreach (var p in ours) harmony.Unpatch(original, p.PatchMethod);
                }
            }
            catch (Exception e)
            {
                GenesisLog.Error("Patch", "rollback of " + patchClass.Name + " failed: " + e);
            }
        }
    }
}
