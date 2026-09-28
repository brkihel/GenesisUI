using System;
using System.Collections.Generic;
using System.IO;
using GenesisUI.Theme;
using Xunit;

namespace GenesisUI.Core.Tests
{
    /// <summary>
    /// Reads the art exactly as the plugin will: the manifest in art/out through the same
    /// parser and validator, and every PNG it names. 0.2.0-preview.1 shipped a manifest the
    /// game read as empty; this runs the real file through the real code path at build time.
    /// </summary>
    public class ShippedArtTests
    {
        private static string ArtOut()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "GenesisUI.sln"))) dir = dir.Parent;
            Assert.NotNull(dir);
            return Path.Combine(dir.FullName, "art", "out");
        }

        public static IEnumerable<object[]> Styles() =>
            new[] { new object[] { "carved" }, new object[] { "gold" } };

        [Theory]
        [MemberData(nameof(Styles))]
        public void Shipped_manifest_parses_validates_and_matches_its_pngs(string style)
        {
            string dir = Path.Combine(ArtOut(), style);
            var errors = new List<string>();
            var manifest = SpriteManifestValidator.Parse(File.ReadAllText(Path.Combine(dir, "sprites.json")), errors);
            Assert.Empty(errors);
            Assert.Empty(SpriteManifestValidator.Validate(manifest));
            Assert.Contains(manifest.sprites, s => s.name == "bar_frame");

            foreach (var s in manifest.sprites)
            {
                string png = Path.Combine(dir, s.file);
                Assert.True(File.Exists(png), png + " missing");
                var header = new byte[24];
                using (var f = File.OpenRead(png)) Assert.Equal(24, f.Read(header, 0, 24));
                int w = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                int h = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                Assert.Equal(s.width * manifest.scale, w);
                Assert.Equal(s.height * manifest.scale, h);
            }
        }

        [Fact]
        public void Unknown_properties_are_reported_not_ignored()
        {
            var errors = new List<string>();
            SpriteManifestValidator.Parse("{\"scale\":2,\"sprites\":[{\"name\":\"a\",\"file\":\"a.png\",\"widht\":4}]}", errors);
            Assert.Contains(errors, e => e.Contains("widht"));
        }

        [Fact]
        public void Non_integer_sizes_are_errors()
        {
            var errors = new List<string>();
            SpriteManifestValidator.Parse("{\"scale\":2.5,\"sprites\":[]}", errors);
            Assert.Contains(errors, e => e.Contains("scale"));
        }
    }
}
