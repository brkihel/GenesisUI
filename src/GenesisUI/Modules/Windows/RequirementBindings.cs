using System.Collections.Generic;
using UnityEngine;

namespace GenesisUI.Modules.Windows
{
    /// <summary>Identity provided by vanilla when it binds a requirement to a live row.</summary>
    internal static class RequirementBindings
    {
        private static readonly Dictionary<Transform, Piece.Requirement> Rows = new Dictionary<Transform, Piece.Requirement>();
        internal static void Bind(Transform root, Piece.Requirement requirement)
        {
            if (root == null) return;
            if (Rows.Count >= 128 && !Rows.ContainsKey(root)) Rows.Clear();
            Rows[root] = requirement;
        }
        internal static Piece.Requirement Get(Transform root) => root != null && Rows.TryGetValue(root, out var requirement) ? requirement : null;
        internal static void Clear() => Rows.Clear();
    }
}
