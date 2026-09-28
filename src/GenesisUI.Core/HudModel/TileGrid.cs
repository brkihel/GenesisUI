namespace GenesisUI.HudModel
{
    /// <summary>
    /// Positions of tiles filled right-to-left and top-to-bottom from a top-right anchor
    /// (status effects), in UI units relative to that anchor.
    /// </summary>
    public static class TileGrid
    {
        public static void RightToLeft(int index, int perRow, float cellWidth, float cellHeight, out float x, out float y)
        {
            if (perRow < 1) perRow = 1;
            if (index < 0) index = 0;
            int row = index / perRow;
            int column = index % perRow;
            x = -column * cellWidth;
            y = -row * cellHeight;
        }
    }
}
