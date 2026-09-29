namespace GenesisUI.Theme
{
    /// <summary>
    /// Colour tokens of docs/ART-DIRECTION.md §2, sampled from the concept art.
    /// Code never uses a literal colour: it asks for a token, so a theme file can
    /// replace all of them at once later.
    /// </summary>
    public sealed class ThemeTokens
    {
        public ColorRgba PanelBackground = ColorRgba.FromHex("#151816EB");
        public ColorRgba LineFrame = ColorRgba.FromHex("#705B3F");
        public ColorRgba AccentGold = ColorRgba.FromHex("#D2AA5E");
        public ColorRgba AccentGoldBright = ColorRgba.FromHex("#D8B76C");
        public ColorRgba TextTitle = ColorRgba.FromHex("#E4D9C0");
        public ColorRgba TextBody = ColorRgba.FromHex("#C2BAAB");
        public ColorRgba TextMuted = ColorRgba.FromHex("#EDE8DDB3");
        public ColorRgba TextFlavor = ColorRgba.FromHex("#98804F");
        public ColorRgba StatePositive = ColorRgba.FromHex("#A2DC88");
        public ColorRgba StateDanger = ColorRgba.FromHex("#E0643C");

        public ColorRgba BarHealth = ColorRgba.FromHex("#A51C1E");
        public ColorRgba BarStamina = ColorRgba.FromHex("#C08A22");
        public ColorRgba BarEitr = ColorRgba.FromHex("#2281AD");

        /// <summary>The trailing "recent change" part of a bar, over the bar colour.</summary>
        public float BarTrailAlpha = 0.45f;

        public static ThemeTokens Default() => new ThemeTokens();
    }
}
