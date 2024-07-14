
using System.Globalization;

namespace Iguina.Defs
{
    /// <summary>
    /// Serializable color value.
    /// </summary>
    public struct Color
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; }

        /// <summary>
        /// Parse color value from string.
        /// </summary>
        /// <param name="hex">Color as string, in format RRGGBB or RRGGBBAA.</param>
        /// <returns>Parsed color.</returns>
        public static Color Parse(string hex)
        {
            byte r, g, b, a = 255;

            if (hex.Length == 6)
            {
                r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            }
            else if (hex.Length == 8)
            {
                r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                a = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
            }
            else
            {
                throw new ArgumentException("Invalid hex color format");
            }

            return new Color(r, g, b, a);
        }

        public Color()
        {
        }

        public Color(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static readonly Color White = new Color(255, 255, 255, 255);
        public static readonly Color Black = new Color(0, 0, 0, 255);
    }
}
