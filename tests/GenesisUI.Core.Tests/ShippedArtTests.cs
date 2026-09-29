using System.Linq;
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

        [Fact]
        public void Shipped_manifest_parses_validates_and_matches_its_pngs()
        {
            string dir = ArtOut();
            var errors = new List<string>();
            var manifest = SpriteManifestValidator.Parse(File.ReadAllText(Path.Combine(dir, "sprites.json")), errors);
            Assert.Empty(errors);
            Assert.Empty(SpriteManifestValidator.Validate(manifest));
            Assert.Contains(manifest.sprites, s => s.name == "bar_frame");
            // The pieces cut from Diego's texture sheets (D-027), with what their views rely on.
            foreach (var name in new[] { "vital_health", "vital_stamina" })
            {
                Assert.Contains(manifest.sprites, s => s.name == name && s.HasContent);
                Assert.Contains(manifest.sprites, s => s.name == name + "_opening");
                Assert.Contains(manifest.sprites, s => s.name == name + "_shape");
            }
            foreach (var name in new[] { "liquid_health", "liquid_stamina", "liquid_eitr", "liquid_health_h", "liquid_stamina_h", "panel_bg" })
                Assert.Contains(manifest.sprites, s => s.name == name && s.wrap == "repeat");
            // D-033: the sheet's heavy hotbar is retired; thin-line frames ship a linear relief map for the metal shader.
            Assert.DoesNotContain(manifest.sprites, s => s.name == "hotbar_frame");
            foreach (var name in new[] { "window_panel", "window_topbar", "window_hintbar", "hotslot", "keycap", "keycap_wide", "card", "tab_marker" })
            {
                var lit = manifest.sprites.Single(s => s.name == name);
                var relief = manifest.sprites.Single(s => s.name == name + "_relief");
                Assert.Equal("linear", relief.color);
                Assert.Equal((lit.width, lit.height, lit.borderLeft, lit.borderTop), (relief.width, relief.height, relief.borderLeft, relief.borderTop));
            }
            Assert.Contains(manifest.sprites, s => s.name == "card_knot_left" && s.inset >= 0);
            // Cell states line their window up with the cell: they need content insets.
            Assert.Contains(manifest.sprites, s => s.name == "slot_selected" && s.HasContent);
            Assert.Contains(manifest.sprites, s => s.name == "slot_equipped" && s.HasContent);
            Assert.Contains(manifest.sprites, s => s.name == "sprint_frame_opening");
            // Every frame's shape has the frame's size and 9-slice, or the background would not line up.
            foreach (var fill in manifest.sprites)
            {
                if (!fill.name.EndsWith("_shape", StringComparison.Ordinal)) continue;
                var frame = Array.Find(manifest.sprites, s => s.name == fill.name.Substring(0, fill.name.Length - 6));
                Assert.NotNull(frame);
                Assert.Equal(frame.width, fill.width);
                Assert.Equal(frame.height, fill.height);
                Assert.Equal(frame.borderLeft, fill.borderLeft);
                Assert.Equal(frame.borderTop, fill.borderTop);
            }

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
