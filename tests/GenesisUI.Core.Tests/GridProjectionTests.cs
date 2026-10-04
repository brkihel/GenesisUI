using System;
using System.Collections.Generic;
using GenesisUI.InventoryModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class GridProjectionTests
    {
        [Theory]
        [InlineData(8, 10, 6)]
        [InlineData(11, 1, 6)]
        [InlineData(1, 4, 6)]
        public void Wrapped_views_retain_every_native_address_once(int width, int height, int columns)
        {
            var view = new GridProjection(width, height, columns);
            var addresses = new HashSet<(int, int)>();
            for (int i = 0; i < view.Count; i++) Assert.True(addresses.Add(view.Native(i)));
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) Assert.Contains((x, y), addresses);
            Assert.InRange(view.Columns, 1, columns);
            Assert.True(view.Rows * view.Columns >= view.Count);
            Assert.True((view.Rows - 1) * view.Columns < view.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => view.Native(view.Count));
        }

        [Fact]
        public void Oversized_or_invalid_foreign_grids_are_rejected_before_allocating_cells()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridProjection(int.MaxValue, 2, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridProjection(0, 2, 6));
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridProjection(1, 1, 0));
        }
    }
}
