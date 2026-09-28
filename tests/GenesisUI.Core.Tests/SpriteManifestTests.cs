using System.Linq;
using GenesisUI.Theme;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class SpriteManifestTests
    {
        private static SpriteManifest Valid() => new SpriteManifest
        {
            scale = 2,
            sprites = new[]
            {
                new SpriteEntry { name = "bar_frame", file = "bar_frame.png", width = 48, height = 160, borderLeft = 10, borderRight = 10, borderTop = 26, borderBottom = 14 },
            },
        };

        [Fact]
        public void The_shipped_shape_is_valid()
        {
            Assert.Empty(SpriteManifestValidator.Validate(Valid()));
        }

        [Theory]
        [InlineData("../evil", "../evil.png")]
        [InlineData("bar_frame", "../../BepInEx/config/x.png")]
        [InlineData("bar_frame", "bar_frame.jpg")]
        [InlineData("Bar", "Bar.png")]
        [InlineData("a/b", "a/b.png")]
        [InlineData("", ".png")]
        public void Names_and_files_cannot_leave_the_art_folder(string name, string file)
        {
            var m = Valid();
            m.sprites[0].name = name;
            m.sprites[0].file = file;
            Assert.NotEmpty(SpriteManifestValidator.Validate(m));
        }

        [Fact]
        public void Borders_larger_than_the_sprite_are_rejected()
        {
            var m = Valid();
            m.sprites[0].borderTop = 150;
            Assert.Contains(SpriteManifestValidator.Validate(m), e => e.Contains("borders"));
        }

        [Fact]
        public void Duplicates_and_empty_manifests_are_rejected()
        {
            var m = Valid();
            m.sprites = new[] { m.sprites[0], m.sprites[0] };
            Assert.Contains(SpriteManifestValidator.Validate(m), e => e.Contains("duplicate"));
            Assert.NotEmpty(SpriteManifestValidator.Validate(null));
            Assert.NotEmpty(SpriteManifestValidator.Validate(new SpriteManifest { scale = 2 }));
        }

        [Fact]
        public void Wrap_is_clamp_or_repeat()
        {
            var m = Valid();
            Assert.Equal("clamp", m.sprites[0].wrap);
            m.sprites[0].wrap = "repeat";
            Assert.Empty(SpriteManifestValidator.Validate(m));
            m.sprites[0].wrap = "mirror";
            Assert.Contains(SpriteManifestValidator.Validate(m), e => e.Contains("wrap"));
        }

        [Fact]
        public void Content_insets_are_all_or_nothing_and_leave_room()
        {
            var m = Valid();
            Assert.False(m.sprites[0].HasContent);

            m.sprites[0].contentLeft = 9; m.sprites[0].contentRight = 9; m.sprites[0].contentBottom = 24; m.sprites[0].contentTop = 32;
            Assert.True(m.sprites[0].HasContent);
            Assert.Empty(SpriteManifestValidator.Validate(m));

            m.sprites[0].contentTop = -1;
            Assert.Contains(SpriteManifestValidator.Validate(m), e => e.Contains("all four"));

            m.sprites[0].contentTop = 140;
            Assert.Contains(SpriteManifestValidator.Validate(m), e => e.Contains("no room"));
        }

        [Fact]
        public void Scale_is_bounded()
        {
            var m = Valid();
            m.scale = 9;
            Assert.Single(SpriteManifestValidator.Validate(m), e => e.Contains("scale"));
        }
    }
}
