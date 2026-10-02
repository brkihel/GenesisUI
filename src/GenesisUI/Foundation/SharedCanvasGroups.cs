using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Foundation
{
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.SharedCanvasGroups), typeof(GenesisUI.Foundation.SharedHideClaims))]
    internal sealed class CanvasGroupLease
    {
        internal SharedCanvasGroups.State State;
        internal int Id;
        internal string Owner;
        internal bool Released;
        internal float? AlphaFactor;
        internal CanvasGroup Group => State.Group;
        internal void Hide(bool hidden, bool? interactable = false)
        {
            if (Released) return;
            State.Claims.SetHidden(Id, hidden, interactable);
            State.RestoreValues(); State.Enforce();
        }
        internal void Fade(float factor) { if (Released) return; AlphaFactor = Mathf.Clamp01(factor); State.Enforce(); }
        internal void Release() => SharedCanvasGroups.Release(this);
    }

    /// <summary>One original snapshot per object, restored only after its last borrower leaves.</summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.CanvasGroupPin), typeof(GenesisUI.Foundation.CanvasGroupLease), typeof(GenesisUI.Foundation.SharedHideClaims), typeof(GenesisUI.Foundation.Guard))]
    internal static class SharedCanvasGroups
    {
        internal sealed class State
        {
            internal GameObject Target;
            internal CanvasGroup Group;
            internal CanvasGroupPin Pin;
            internal bool Added;
            internal float Alpha;
            internal bool Raycasts, Interactable, Ignore;
            internal readonly SharedHideClaims Claims = new SharedHideClaims();
            internal readonly List<CanvasGroupLease> Leases = new List<CanvasGroupLease>();
            internal void Enforce()
            {
                if (Group == null) return;
                float factor = 1f; bool fading = false;
                foreach (var lease in Leases) if (lease.AlphaFactor.HasValue) { factor = Mathf.Min(factor, lease.AlphaFactor.Value); fading = true; }
                if (fading || Claims.Hidden) Group.alpha = Claims.Hidden ? 0f : Alpha * factor;
                if (!Claims.Hidden) return;
                if (Group.alpha != 0f) Group.alpha = 0f;
                if (Group.blocksRaycasts) Group.blocksRaycasts = false;
                if (Claims.Interactable.HasValue && Group.interactable != Claims.Interactable.Value)
                    Group.interactable = Claims.Interactable.Value;
            }
            internal void RestoreValues()
            {
                if (Group == null) return;
                Group.alpha = Alpha;
                Group.blocksRaycasts = Raycasts;
                Group.interactable = Interactable;
                Group.ignoreParentGroups = Ignore;
            }
        }
        private static readonly Dictionary<GameObject, State> States = new Dictionary<GameObject, State>();

        internal static CanvasGroupLease Acquire(string owner, GameObject target)
        {
            if (target == null) return null;
            if (!States.TryGetValue(target, out var state) || state.Group == null)
            {
                var group = target.GetComponent<CanvasGroup>();
                state = new State { Target = target, Added = group == null };
                state.Group = group != null ? group : target.AddComponent<CanvasGroup>();
                state.Alpha = state.Group.alpha;
                state.Raycasts = state.Group.blocksRaycasts;
                state.Interactable = state.Group.interactable;
                state.Ignore = state.Group.ignoreParentGroups;
                States[target] = state;
                state.Pin = target.AddComponent<CanvasGroupPin>();
                state.Pin.State = state;
            }
            var lease = new CanvasGroupLease { Owner = owner, State = state, Id = state.Claims.Acquire() };
            state.Leases.Add(lease);
            return lease;
        }

        internal static void Release(CanvasGroupLease lease)
        {
            if (lease == null || lease.Released) return;
            lease.Released = true;
            var state = lease.State;
            state.Leases.Remove(lease);
            state.Claims.Release(lease.Id);
            if (state.Claims.Count > 0) { state.RestoreValues(); state.Enforce(); return; }
            if (States.TryGetValue(state.Target, out var current) && ReferenceEquals(current, state)) States.Remove(state.Target);
            state.RestoreValues();
            if (state.Pin != null) Object.DestroyImmediate(state.Pin);
            if (state.Added && state.Group != null) Object.DestroyImmediate(state.Group);
        }

        internal static void ReleaseEveryOwner()
        {
            foreach (var state in new List<State>(States.Values))
                foreach (var lease in state.Leases.ToArray()) Guard.Try("release shared group " + lease.Owner, lease.Release);
            States.Clear();
        }
    }

    [DefaultExecutionOrder(32000)]
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.SharedCanvasGroups), typeof(GenesisUI.Foundation.CanvasGroupLease), typeof(GenesisUI.Foundation.Guard))]
    internal sealed class CanvasGroupPin : MonoBehaviour
    {
        internal SharedCanvasGroups.State State;
        private void LateUpdate()
        {
            if (State == null) return;
            try { State.Enforce(); }
            catch (System.Exception e)
            {
                enabled = false;
                foreach (var lease in State.Leases.ToArray()) Guard.Fault(lease.Owner, e);
            }
        }
    }
}
