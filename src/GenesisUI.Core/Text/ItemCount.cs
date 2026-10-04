using System.Globalization;

namespace GenesisUI.Text
{
    /// <summary>Keep large native stack counts readable inside compact item cells.</summary>
    public static class ItemCount
    {
        public static string Compact(int count)
        {
            if (count <= 0) return "";
            if (count >= 1000000000) return (count / 1000000000d).ToString("0.#", CultureInfo.InvariantCulture) + "G";
            if (count >= 1000000) return (count / 1000000d).ToString("0.#", CultureInfo.InvariantCulture) + "M";
            if (count >= 10000) return (count / 1000d).ToString("0.#", CultureInfo.InvariantCulture) + "k";
            return count.ToString(CultureInfo.InvariantCulture);
        }
    }
}
