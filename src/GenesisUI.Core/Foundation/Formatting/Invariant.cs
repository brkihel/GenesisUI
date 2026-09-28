using System.Globalization;

namespace GenesisUI.Foundation.Formatting
{
    /// <summary>
    /// Every number GenesisUI writes or reads goes through here. pt-BR uses a comma
    /// as the decimal separator; parsing "0.117" with the player's culture has
    /// already turned a 11.7% drop into 100% on this server.
    /// </summary>
    public static class Invariant
    {
        public static string Format(double value, int decimals)
        {
            if (decimals < 0) decimals = 0;
            if (decimals > 6) decimals = 6;
            return value.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }

        public static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

        public static bool TryParse(string text, out double value) =>
            double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
