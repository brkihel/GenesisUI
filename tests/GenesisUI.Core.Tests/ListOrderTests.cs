using System.Collections.Generic;
using GenesisUI.Collections;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class ListOrderTests
    {
        [Fact]
        public void Matching_items_come_first_in_their_order_and_nothing_is_lost()
        {
            var list = new List<int> { 1, 2, 3, 4, 5, 6 };
            ListOrder.StablePartition(list, x => x % 2 == 0, new List<int>());
            Assert.Equal(new[] { 2, 4, 6, 1, 3, 5 }, list);
        }

        [Fact]
        public void Mixed_lists_like_the_crafting_columns_do_not_throw()
        {
            var list = new List<bool> { false, true, false, true, true };
            ListOrder.StablePartition(list, x => x, new List<bool>());
            Assert.Equal(new[] { true, true, true, false, false }, list);
        }
    }
}
