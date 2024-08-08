using Iguina.Defs;
using Iguina.Drivers;

namespace Iguina.Tests
{
    public class TestRenderer : IRenderer
    {
        private Rectangle? _scissorRegion;

        
        public Rectangle GetScreenBounds()
        {
            return new Rectangle(0, 0, 1280, 720);
        }

        public void DrawTexture(string? effectIdentifier, string textureId, Rectangle destRect, Rectangle sourceRect, Color color)
        {
        }

        public Point MeasureText(string text, string? fontId, int fontSize, float spacing)
        {
            return new Point(text.Length * 10, 20);
        }

        public int GetTextLineHeight(string? fontId, int fontSize)
        {
            return 20;
        }

        public void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing)
        {
        }

        public void DrawRectangle(Rectangle rectangle, Color color)
        {
        }

        public void SetScissorRegion(Rectangle region)
        {
            _scissorRegion = region;
        }

        public Rectangle? GetScissorRegion()
        {
            return _scissorRegion;
        }

        public void ClearScissorRegion()
        {
            _scissorRegion = null;
        }

        public Color GetPixelFromTexture(string textureId, Point sourcePosition)
        {
            throw new NotImplementedException();
        }

        public Point? FindPixelOffsetInTexture(string textureId, Rectangle sourceRect, Color color, bool returnNearestColor)
        {
            throw new NotImplementedException();
        }
    }
}