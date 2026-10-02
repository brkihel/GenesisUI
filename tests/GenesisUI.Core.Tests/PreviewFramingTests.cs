using System;
using GenesisUI.Geometry;
using Xunit;

namespace GenesisUI.Core.Tests
{
    public class PreviewFramingTests
    {
        [Theory]
        [InlineData(0.36f)]
        [InlineData(0.7f)]
        [InlineData(1.777f)]
        public void Every_corner_of_the_character_box_is_inside_the_camera(float aspect)
        {
            double angle = 4 * Math.PI / 180;
            double tanV = Math.Tan(12 * Math.PI / 180);
            float distance = PreviewFraming.Distance(0.9f, 2f, 0.7f, 4f, 24f, aspect, 1.06f);
            foreach (int x in new[] {-1, 1})
            foreach (int y in new[] {-1, 1})
            foreach (int z in new[] {-1, 1})
            {
                double projectedY = y * Math.Cos(angle) - z * 0.35 * Math.Sin(angle);
                double cameraDepth = distance + y * Math.Sin(angle) + z * 0.35 * Math.Cos(angle);
                Assert.InRange(Math.Abs(projectedY) / cameraDepth, 0, tanV);
                Assert.InRange(Math.Abs(x * 0.45) / cameraDepth, 0, tanV * aspect);
            }
        }

        [Fact]
        public void A_narrow_panel_changes_distance_only_when_width_requires_it()
        {
            float portrait = PreviewFraming.Distance(0.9f, 2f, 0.7f, 4, 24, 0.7f, 1.06f);
            float landscape = PreviewFraming.Distance(0.9f, 2f, 0.7f, 4, 24, 1.777f, 1.06f);
            Assert.Equal(landscape, portrait); // height controls both; the character keeps the same size
            Assert.True(PreviewFraming.Distance(0.9f, 2f, 0.7f, 4, 24, 0.2f, 1.06f) > portrait);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(-1f)]
        [InlineData(float.NaN)]
        public void Invalid_panel_aspect_cannot_generate_an_invalid_camera(float aspect)
            => Assert.Throws<ArgumentOutOfRangeException>(() => PreviewFraming.Distance(1, 2, 1, 4, 24, aspect, 1));
    }
}
