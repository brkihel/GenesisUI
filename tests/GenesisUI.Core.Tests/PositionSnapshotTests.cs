using System;
using System.Collections.Generic;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class PositionSnapshotTests
    {
        private sealed class Item { internal int X, Y, Count = 1; }
        private static PositionSnapshot<Item> Snapshot(List<Item> items) => new PositionSnapshot<Item>(items, i => i.Count, i => (i.X, i.Y));
        private static void Write(Item item, int x, int y) { item.X = x; item.Y = y; }

        [Fact]
        public void Rollback_refuses_a_restore_chain_blocked_by_a_new_foreign_occupant()
        {
            var a = new Item(); var b = new Item { X = 1 }; var live = new List<Item> { a, b };
            var snapshot = Snapshot(live);
            snapshot.Apply(live, new[] { new Move { Id = 0, X = 1 }, new Move { Id = 1, X = 2 } }, 8, 9, Write);
            var foreign = new Item { X = 0 }; live.Add(foreign);
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(1, a.X); Assert.Equal(2, b.X); Assert.Equal(0, foreign.X);
        }

        [Fact]
        public void Rollback_restores_a_swap_when_both_positions_are_still_ours()
        {
            var a = new Item(); var b = new Item { X = 1 }; var live = new List<Item> { a, b };
            var snapshot = Snapshot(live);
            snapshot.Apply(live, new[] { new Move { Id = 0, X = 1 }, new Move { Id = 1, X = 0 } }, 8, 9, Write);
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(0, a.X); Assert.Equal(1, b.X);
        }

        [Fact]
        public void A_write_that_throws_after_mutation_is_still_journaled()
        {
            var a = new Item(); var live = new List<Item> { a }; var snapshot = Snapshot(live);
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(live, new[] { new Move { Id = 0, X = 3 } }, 8, 9, (item, x, y) => { Write(item, x, y); throw new InvalidOperationException("after write"); }));
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(0, a.X);
        }

        [Fact]
        public void Reordered_list_keeps_plan_bound_to_original_items_and_rollback_restores_them()
        {
            Item a = new Item(), b = new Item { X = 1 };
            var live = new List<Item> { a, b };
            var snapshot = Snapshot(live);
            live.Reverse();
            snapshot.Apply(live, new[]{new Move { Id = 0, X = 2, Y = 1 }}, 8, 9, Write);
            Assert.Equal(2, a.X);
            Assert.Equal(1, b.X);
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(0, a.X);
        }

        [Fact]
        public void Membership_or_count_change_refuses_the_plan_before_any_write()
        {
            Item a = new Item(), b = new Item { X = 1 };
            var live = new List<Item> { a, b };
            var snapshot = Snapshot(live);
            b.Count++;
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(live, new[]{new Move { Id = 0, X = 3 }}, 8, 9, Write));
            Assert.Equal(0, a.X);
            b.Count--;
            live[1] = new Item { X = 1 };
            Assert.False(snapshot.Matches(live));
        }

        [Fact]
        public void Full_plan_is_validated_and_removed_items_are_not_resurrected_on_rollback()
        {
            Item a = new Item(), b = new Item { X = 1 };
            var live = new List<Item> { a, b };
            var snapshot = Snapshot(live);
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(live, new[]{new Move { Id = 0, X = 1 }}, 8, 9, Write));
            Assert.Equal(0, a.X);
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(live, new[]{new Move { Id = 0, X = 0, Y = 9 }}, 8, 9, Write));
            live.Remove(b); b.X = 5;
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(5, b.X);
            Assert.Single(live);
        }

        [Fact]
        public void Layout_rejects_duplicate_logical_ids()
        {
            var layout = new SlotLayout(4, 4, 4, null);
            var plan = LayoutChange.Compute(layout, layout, new[]{new ItemAt(0, 0, 0), new ItemAt(0, 1, 0)});
            Assert.False(plan.Ok);
            Assert.Contains("duplicate", plan.Reason);
            Assert.Empty(plan.Moves);
        }
        [Fact]
        public void Position_changed_after_planning_is_rejected_before_overwriting_foreign_work()
        {
            var item = new Item(); var live = new List<Item> { item }; var snapshot = Snapshot(live);
            item.X = 2;
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(live, new[] { new Move { Id = 0, X = 3 } }, 8, 9, Write));
            Assert.Equal(2, item.X);
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(2, item.X);
        }
        [Fact]
        public void Rollback_preserves_a_later_foreign_position_and_never_recreates_items()
        {
            var item = new Item(); var live = new List<Item> { item }; var snapshot = Snapshot(live);
            snapshot.Apply(live, new[] { new Move { Id = 0, X = 3 } }, 8, 9, Write);
            item.X = 4;
            snapshot.RestoreRemaining(live, Write);
            Assert.Equal(4, item.X);
        }
    }
}
