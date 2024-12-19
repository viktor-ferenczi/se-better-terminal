using System;
using System.Text.RegularExpressions;
using VRageMath;

namespace ClientPlugin.Tools
{
    public static class Tools
    {
        private static readonly Regex RxHexColorRgbRegex = new Regex("([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})", RegexOptions.IgnoreCase);
        private static readonly Regex RxHexColorRgbaRegex = new Regex("([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})", RegexOptions.IgnoreCase);

        public static string ToHexStringRgb(this Color color)
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static string ToHexStringRgba(this Color color)
        {
            return $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
        }

        public static bool TryParseColorFromHexRgb(this string hex, out Color color)
        {
            var match = RxHexColorRgbRegex.Match(hex);
            if (!match.Success)
            {
                color = Color.Black;
                return false;
            }

            color = Color.FromNonPremultiplied(
                Convert.ToInt16(match.Groups[1].Value, 16),
                Convert.ToInt16(match.Groups[2].Value, 16),
                Convert.ToInt16(match.Groups[3].Value, 16),
                255);
            return true;
        }

        public static bool TryParseColorFromHexRgba(this string hex, out Color color)
        {
            var match = RxHexColorRgbaRegex.Match(hex);
            if (!match.Success)
            {
                color = Color.Black;
                return false;
            }

            color = Color.FromNonPremultiplied(
                Convert.ToInt16(match.Groups[1].Value, 16),
                Convert.ToInt16(match.Groups[2].Value, 16),
                Convert.ToInt16(match.Groups[3].Value, 16),
                Convert.ToInt16(match.Groups[4].Value, 16));
            return true;
        }
    }
}