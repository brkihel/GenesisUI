using GenesisUI.Foundation.Input;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class LeaseBookTests
    {
        private static (LeaseBook book, int[] counts) Book()
        {
            var book = new LeaseBook();
            var counts = new int[2];
            book.FirstAcquired += () => counts[0]++;
            book.LastReleased += () => counts[1]++;
            return (book, counts);
        }

        [Fact]
        public void Block_is_requested_once_and_released_once()
        {
            var (book, counts) = Book();

            long a = book.Acquire("window:inventory");
            long b = book.Acquire("window:crafting");
            book.Release(a);
            Assert.Equal(new[] { 1, 0 }, counts);

            book.Release(b);
            Assert.Equal(new[] { 1, 1 }, counts);
            Assert.Equal(0, book.ActiveCount);
        }

        [Fact]
        public void Release_is_idempotent()
        {
            var (book, counts) = Book();
            long a = book.Acquire("o");

            Assert.True(book.Release(a));
            Assert.False(book.Release(a));
            Assert.False(book.Release(12345));
            Assert.Equal(1, counts[1]);
        }

        [Fact]
        public void Release_all_frees_only_that_owner()
        {
            var (book, counts) = Book();
            book.Acquire("faulty");
            book.Acquire("faulty");
            book.Acquire("healthy");

            Assert.Equal(2, book.ReleaseAll("faulty"));
            Assert.Equal(1, book.ActiveCount);
            Assert.Equal(0, counts[1]); // input stays blocked for the healthy owner
        }

        [Fact]
        public void Release_all_of_the_last_owner_unblocks()
        {
            var (book, counts) = Book();
            book.Acquire("faulty");

            book.ReleaseAll("faulty");
            Assert.Equal(1, counts[1]);
            Assert.Equal(0, book.ReleaseAll("faulty"));
            Assert.Equal(1, counts[1]);
        }
    }
}
