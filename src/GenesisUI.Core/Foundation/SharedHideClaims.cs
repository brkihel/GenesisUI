using System.Collections.Generic;

namespace GenesisUI.Foundation
{
    /// <summary>Independent claims on one shared drawing group; false interaction wins.</summary>
    public sealed class SharedHideClaims
    {
        private struct Claim { public bool Hidden; public bool? Interactable; }
        private readonly Dictionary<int, Claim> _claims = new Dictionary<int, Claim>();
        private int _next;
        public int Count => _claims.Count;
        public bool Hidden { get; private set; }
        public bool? Interactable { get; private set; }

        public int Acquire() { int id = ++_next; _claims.Add(id, default); return id; }
        public void SetHidden(int id, bool hidden, bool? interactable)
        {
            if (!_claims.ContainsKey(id)) return;
            _claims[id] = new Claim { Hidden = hidden, Interactable = interactable };
            Recompute();
        }
        public void Release(int id) { if (_claims.Remove(id)) Recompute(); }

        private void Recompute()
        {
            Hidden = false;
            Interactable = null;
            foreach (var claim in _claims.Values)
            {
                if (!claim.Hidden) continue;
                Hidden = true;
                if (claim.Interactable == false) Interactable = false;
                else if (claim.Interactable == true && Interactable == null) Interactable = true;
            }
        }
    }
}
