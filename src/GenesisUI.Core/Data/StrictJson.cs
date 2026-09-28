using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GenesisUI.Data
{
    /// <summary>
    /// A small, strict JSON reader for GenesisUI's own data files (docs/DECISIONS.md D-018).
    /// Objects become Dictionary&lt;string, object&gt;, arrays List&lt;object&gt;, numbers double,
    /// plus string, bool and null. Anything malformed throws with a position, nothing is
    /// ever half-parsed, and size and depth are bounded. It never creates types or calls
    /// code: data stays data.
    /// </summary>
    public static class StrictJson
    {
        public const int MaxLength = 256 * 1024;
        public const int MaxDepth = 32;

        public static object Parse(string text)
        {
            if (text == null) throw new FormatException("JSON: no text");
            if (text.Length > MaxLength) throw new FormatException("JSON: larger than " + MaxLength + " characters");
            var p = new Parser(text);
            p.SkipWhitespace();
            object value = p.ReadValue(0);
            p.SkipWhitespace();
            if (!p.AtEnd) throw p.Error("unexpected content after the value");
            return value;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s)
            {
                _s = s;
                // A UTF-8 BOM read as text is harmless; skip it.
                if (_s.Length > 0 && _s[0] == '﻿') _i = 1;
            }

            public bool AtEnd => _i >= _s.Length;

            public FormatException Error(string what) => new FormatException("JSON: " + what + " at position " + _i);

            public void SkipWhitespace()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\t' || _s[_i] == '\n' || _s[_i] == '\r')) _i++;
            }

            public object ReadValue(int depth)
            {
                if (depth > MaxDepth) throw Error("nested deeper than " + MaxDepth);
                if (AtEnd) throw Error("unexpected end");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ReadObject(depth);
                    case '[': return ReadArray(depth);
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw Error("unexpected character '" + c + "'");
                }
            }

            private Dictionary<string, object> ReadObject(int depth)
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _i++; // {
                SkipWhitespace();
                if (Peek('}')) { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (!Peek('"')) throw Error("expected a property name");
                    string key = ReadString();
                    if (result.ContainsKey(key)) throw Error("duplicate property '" + key + "'");
                    SkipWhitespace();
                    if (!Peek(':')) throw Error("expected ':'");
                    _i++;
                    SkipWhitespace();
                    result[key] = ReadValue(depth + 1);
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    if (Peek('}')) { _i++; return result; }
                    throw Error("expected ',' or '}'");
                }
            }

            private List<object> ReadArray(int depth)
            {
                var result = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (Peek(']')) { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    result.Add(ReadValue(depth + 1));
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    if (Peek(']')) { _i++; return result; }
                    throw Error("expected ',' or ']'");
                }
            }

            private string ReadString()
            {
                _i++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw Error("unterminated string");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c < 0x20) throw Error("control character in string");
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("unterminated escape");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length || !ushort.TryParse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort code))
                                throw Error("bad \\u escape");
                            sb.Append((char)code);
                            _i += 4;
                            break;
                        default: throw Error("unknown escape \\" + e);
                    }
                }
            }

            private double ReadNumber()
            {
                int start = _i;
                if (Peek('-')) _i++;
                while (!AtEnd && ((_s[_i] >= '0' && _s[_i] <= '9') || _s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-')) _i++;
                string token = _s.Substring(start, _i - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || double.IsInfinity(value))
                    throw Error("bad number '" + token + "'");
                return value;
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error("expected '" + word + "'");
                _i += word.Length;
            }

            private bool Peek(char c) => _i < _s.Length && _s[_i] == c;
        }
    }
}
