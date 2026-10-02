using System.Collections.Generic;
namespace GenesisUI.Data
{
    public static class TextMapFingerprint
    {
        public static ulong Of(IEnumerable<KeyValuePair<string,string>> entries)
        {
            if (entries == null) return 0;
            ulong result = 0, count = 0;
            unchecked
            {
                foreach (var pair in entries)
                {
                    ulong hash = 14695981039346656037UL;
                    if (pair.Key != null) foreach (char c in pair.Key) hash = (hash ^ c) * 1099511628211UL;
                    hash = (hash ^ 0xff) * 1099511628211UL;
                    if (pair.Value != null) foreach (char c in pair.Value) hash = (hash ^ c) * 1099511628211UL;
                    result += hash; count++;
                }
                return result ^ count * 1099511628211UL;
            }
        }
    }
}
