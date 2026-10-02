using System;

namespace GenesisUI.Data
{
    /// <summary>Reject excessive decoded image sizes before asking Unity to allocate a texture.</summary>
    public sealed class PngBudget
    {
        public const int MaxDimension = 4096;
        public const long MaxDecodedBytes = 64L * 1024 * 1024;
        public long ReservedBytes { get; private set; }
        public bool Reserve(byte[] header, int width, int height)
        {
            if (header == null || header.Length < 33 || width <= 0 || height <= 0) return false;
            byte[] signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
            for (int i = 0; i < 8; i++) if (header[i] != signature[i]) return false;
            if (Read(header, 8) != 13 || header[12] != 'I' || header[13] != 'H' || header[14] != 'D' || header[15] != 'R') return false;
            if (Read(header, 16) != width || Read(header, 20) != height || width > MaxDimension || height > MaxDimension) return false;
            long bytes = (long)width * height * 4;
            if (bytes > MaxDecodedBytes - ReservedBytes) return false;
            ReservedBytes += bytes; return true;
        }
        private static long Read(byte[] bytes, int offset) => ((long)bytes[offset] << 24) | ((long)bytes[offset + 1] << 16) | ((long)bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
