using System;
using UnityEngine;

namespace GenesisUI.Foundation
{
    /// <summary>Independent Unity callbacks retain the owner that created their component.</summary>
    [GenesisUI.Foundation.Contracts.ContractDependency(typeof(GenesisUI.Foundation.Guard))]
    internal abstract class GuardedBehaviour : MonoBehaviour
    {
        protected string CallbackOwner { get; private set; }
        private static readonly Action<GuardedBehaviour> Tick = behaviour => behaviour.OnOwnerUpdate();
        private static readonly Action<GuardedBehaviour> LateTick = behaviour => behaviour.OnOwnerLateUpdate();
        private void Awake() { CallbackOwner = Guard.CurrentOwner ?? "shared:ui-callback"; }
        private void Update() => Guard.Run(CallbackOwner, Tick, this);
        private void LateUpdate() => Guard.Run(CallbackOwner, LateTick, this);
        private void OnEnable() => Guard.Run(CallbackOwner, OnOwnerEnabled);
        private void OnDisable() => Guard.Try("disable " + CallbackOwner, OnOwnerDisabled);
        private void OnDestroy() => Guard.Try("destroy " + CallbackOwner, OnOwnerDestroyed);
        protected virtual void OnOwnerUpdate() { }
        protected virtual void OnOwnerLateUpdate() { }
        protected virtual void OnOwnerEnabled() { }
        protected virtual void OnOwnerDisabled() { }
        protected virtual void OnOwnerDestroyed() { }
    }
}
