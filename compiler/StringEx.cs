using System;
using System.Drawing;
using Pastel;

namespace wave
{
    public static class StringEx
    {
        private static readonly Random rnd = new Random();
        public static string Emoji(this string str)
        {
            if (Environment.GetEnvironmentVariable("RUNE_EMOJI_USE") == "0")
                return "";
            return EmojiOne.EmojiOne.ShortnameToUnicode(str);
        }

        public static string Color(this string str, Color color)
        {
            if (Environment.GetEnvironmentVariable("RUNE_COLOR_USE") == "0")
                return str;
            return str.Pastel(color);
        }

        public static string Nier(this string str, int? index = null, int depth = 0)
        {
            if (Environment.GetEnvironmentVariable("RUNE_NIER_USE") == "0")
                return str;
            if (depth > 5) return str;
            if (index is null)
                index = rnd.Next(0, str.Length - 1);
            var @char = str[index.Value];
            if (!char.IsLetter(@char))
                return str.Nier(null, ++depth);
            return str.Remove(index.Value, 1).Insert(index.Value, $"[{$"{@char}".ToUpperInvariant()}]");
        }
    }
}