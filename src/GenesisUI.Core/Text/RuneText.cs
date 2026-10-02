using System;

namespace GenesisUI.Text
{
    /// <summary>Reusable rune-to-letter buffer. Rich text tags, punctuation and line breaks remain intact.</summary>
    public sealed class RuneText
    {
        public const int Limit = 16384;
        private readonly char[] _original;
        private readonly bool[] _letters;
        private int _revealed = -1;
        public char[] Buffer { get; }
        public int LetterCount { get; }
        public float Duration => Math.Min(8f, Math.Max(2.5f, LetterCount * 0.035f));

        public RuneText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (text.Length > Limit) text = text.Substring(0, Limit - 1) + "…";
            _original = text.ToCharArray();
            Buffer = new char[_original.Length];
            _letters = new bool[_original.Length];
            int count = 0;
            for (int i = 0; i < _original.Length; i++)
            {
                if (_original[i] == '<' && i + 1 < _original.Length && (char.IsLetter(_original[i + 1]) || _original[i + 1] == '/'))
                {
                    int end = text.IndexOf('>', i + 1);
                    if (end >= 0) { i = end; continue; }
                }
                if (Rune(_original[i]) != _original[i]) { _letters[i] = true; count++; }
            }
            LetterCount = count;
        }

        public bool Write(float progress)
        {
            int revealed = float.IsNaN(progress) || progress <= 0f ? 0 : progress >= 1f ? LetterCount : (int)(progress * LetterCount);
            if (revealed == _revealed) return false;
            _revealed = revealed;
            int letter = 0;
            for (int i = 0; i < _original.Length; i++)
                Buffer[i] = _letters[i] && letter++ >= revealed ? Rune(_original[i]) : _original[i];
            return true;
        }

        public static char Rune(char c)
        {
            switch (char.ToUpperInvariant(c))
            {
                case 'A': case 'Á': case 'À': case 'Â': case 'Ã': case 'Ä': return 'ᚨ';
                case 'B': return 'ᛒ';
                case 'C': case 'Ç': case 'K': case 'Q': return 'ᚲ';
                case 'D': return 'ᛞ';
                case 'E': case 'É': case 'È': case 'Ê': case 'Ë': return 'ᛖ';
                case 'F': return 'ᚠ';
                case 'G': return 'ᚷ';
                case 'H': return 'ᚺ';
                case 'I': case 'Í': case 'Ì': case 'Î': case 'Ï': return 'ᛁ';
                case 'J': case 'Y': return 'ᛃ';
                case 'L': return 'ᛚ';
                case 'M': return 'ᛗ';
                case 'N': return 'ᚾ';
                case 'O': case 'Ó': case 'Ò': case 'Ô': case 'Õ': case 'Ö': return 'ᛟ';
                case 'P': return 'ᛈ';
                case 'R': return 'ᚱ';
                case 'S': return 'ᛊ';
                case 'T': return 'ᛏ';
                case 'U': case 'Ú': case 'Ù': case 'Û': case 'Ü': return 'ᚢ';
                case 'V': case 'W': return 'ᚹ';
                case 'X': case 'Z': return 'ᛉ';
                default: return c;
            }
        }
    }
}
