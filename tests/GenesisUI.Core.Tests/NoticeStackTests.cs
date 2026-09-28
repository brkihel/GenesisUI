using GenesisUI.HudModel;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class NoticeStackTests
    {
        private static readonly object Wood = new object();
        private static readonly object Stone = new object();

        [Fact]
        public void Newest_is_on_top_and_a_fourth_notice_sends_the_oldest_away()
        {
            var s = new NoticeStack();
            s.Push("a", null);
            s.Push("b", null);
            s.Push("c", null);
            Assert.Equal(new[] { "c", "b", "a" }, Texts(s));
            Assert.All(s.Items, n => Assert.False(n.Leaving));

            s.Push("d", null);
            Assert.Equal(new[] { "d", "c", "b", "a" }, Texts(s));
            Assert.True(s.Items[3].Leaving);
            Assert.False(s.Items[2].Leaving);

            s.Tick(NoticeStack.LeaveSeconds);
            Assert.Equal(new[] { "d", "c", "b" }, Texts(s));
        }

        [Fact]
        public void A_notice_stays_then_fades_while_dropping_then_disappears()
        {
            var s = new NoticeStack();
            s.Push("a", null);
            s.Tick(NoticeStack.ShowSeconds - 0.01f);
            Assert.False(s.Items[0].Leaving);
            Assert.Equal(1f, s.Items[0].Alpha);

            s.Tick(0.02f);
            s.Tick(NoticeStack.LeaveSeconds / 2f);
            var n = s.Items[0];
            Assert.True(n.Leaving);
            Assert.InRange(n.Alpha, 0.3f, 0.7f);
            Assert.InRange(n.Drop, 0.3f, 0.7f);

            s.Tick(NoticeStack.LeaveSeconds);
            Assert.Empty(s.Items);
        }

        [Fact]
        public void A_repeat_of_the_top_notice_updates_it_instead_of_stacking()
        {
            var s = new NoticeStack();
            Assert.True(s.Push("Madeira", Wood));
            s.Tick(1f);
            Assert.False(s.Push("Madeira x5", Wood));
            Assert.Single(s.Items);
            Assert.Equal("Madeira x5", s.Items[0].Text);
            Assert.True(s.Items[0].Age < 1f);

            Assert.True(s.Push("Pedra", Stone));        // another item stacks
            Assert.True(s.Push("Madeira x2", Wood));    // and the old one is no longer on top
            Assert.Equal(3, s.Items.Count);
        }

        [Fact]
        public void An_old_repeat_stacks_as_a_new_notice()
        {
            var s = new NoticeStack();
            s.Push("Abrigado", null);
            s.Tick(NoticeStack.MergeSeconds + 0.1f);
            Assert.True(s.Push("Abrigado", null));
        }

        [Theory]
        [InlineData("Madeira", "Madeira x5", true)]
        [InlineData("Madeira x5", "Madeira x10", true)]
        [InlineData("Madeira", "Madeira", true)]
        [InlineData("Madeira", "Madeiras", false)]
        [InlineData("Nível 5", "Nível 6", false)]   // digits without " x" are part of the text
        [InlineData("Box", "Bo x2", false)]
        public void Same_base_ignores_only_the_amount_suffix(string a, string b, bool same) =>
            Assert.Equal(same, NoticeStack.SameBase(a, b));

        [Fact]
        public void Empty_text_and_bad_time_are_ignored()
        {
            var s = new NoticeStack();
            Assert.False(s.Push("", null));
            s.Push("a", null);
            s.Tick(float.NaN);
            s.Tick(-1f);
            Assert.Equal(0f, s.Items[0].Age);
        }

        private static string[] Texts(NoticeStack s)
        {
            var r = new string[s.Items.Count];
            for (int i = 0; i < r.Length; i++) r[i] = s.Items[i].Text;
            return r;
        }
    }
}
