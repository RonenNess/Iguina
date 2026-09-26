using Iguina.Defs;
using Iguina.Drivers;

namespace Iguina.Tests
{
    /// <summary>
    /// Renderer with a non-monospace font, that records drawn texts and rectangles.
    /// </summary>
    public class ProportionalRenderer : TestRenderer, IRenderer
    {
        public List<(string Text, Point Position)> DrawnTexts = new();
        public List<(Rectangle Rect, Color Color)> DrawnRectangles = new();

        public static int CharWidth(char c) => c switch
        {
            'i' or 'l' or '.' or ' ' or '|' => 4,
            'W' or 'M' or 'm' => 16,
            _ => 9
        };

        public static int Measure(string text) => text.Sum(CharWidth);

        public new Point MeasureText(string text, string? fontId, int fontSize, float spacing)
        {
            return new Point(Measure(text), 20);
        }

        public new void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
        {
            DrawnTexts.Add((text, position));
        }

        public new void DrawRectangle(Rectangle rectangle, Color color)
        {
            DrawnRectangles.Add((rectangle, color));
        }

        public void ClearRecords()
        {
            DrawnTexts.Clear();
            DrawnRectangles.Clear();
        }
    }
}
