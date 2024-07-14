using Iguina.Defs;


namespace Iguina.Drivers
{
    /// <summary>
    /// An interface to provide rendering functionality for the GUI system.
    /// This is one of the drivers your application needs to provide.
    /// </summary>
    public interface IRenderer
    {
        /// <summary>
        /// Get screen bounds, in pixels.
        /// </summary>
        /// <returns>Current screen bounds.</returns>
        Rectangle GetScreenBounds();

        /// <summary>
        /// Draw a texture.
        /// </summary>
        /// <param name="effectIdentifier">Effect to use, or null for default.</param>
        /// <param name="textureId">Texture id to draw.</param>
        /// <param name="destRect">Destination rectangle.</param>
        /// <param name="sourceRect">Source rectangle.</param>
        /// <param name="color">Rendering color.</param>
        void DrawTexture(string? effectIdentifier, string textureId, Rectangle destRect, Rectangle sourceRect, Color color);

        /// <summary>
        /// Measure text size.
        /// </summary>
        /// <param name="text">Text to measure.</param>
        /// <param name="fontId">Font identifier.</param>
        /// <param name="fontSize">Font size.</param>
        /// <param name="spacing">Font spacing factor. 1f = default font spacing.</param>
        /// <returns>Text size, in pixels.</returns>
        Point MeasureText(string text, string? fontId, int fontSize, float spacing);

        /// <summary>
        /// Get line height for a font id and size.
        /// </summary>
        /// <param name="fontId">Font identifier.</param>
        /// <param name="fontSize">Font size.</param>
        /// <returns>Text line height.</returns>
        int GetTextLineHeight(string? fontId, int fontSize);

        /// <summary>
        /// Draw text.
        /// </summary>
        /// <param name="effectIdentifier">Effect to use, or null for default.</param>
        /// <param name="text">Text to render.</param>
        /// <param name="fontId">Font identifier to use, or null for default font.</param>
        /// <param name="fontSize">Font size.</param>
        /// <param name="position">Text position.</param>
        /// <param name="fillColor">Text fill color.</param>
        /// <param name="outlineColor">Text outline color.</param>
        /// <param name="outlineWidth">Outline width, in pixels.</param>
        /// <param name="spacing">Font spacing factor. 1f = default font spacing.</param>
        void DrawText(string? effectIdentifier, string text, string? fontId, int fontSize, Point position, Color fillColor, Color outlineColor, int outlineWidth, float spacing);

        /// <summary>
        /// Draw a filled rectangle.
        /// </summary>
        /// <param name="rectangle">Rectangle region.</param>
        /// <param name="color">Rectangle color.</param>
        void DrawRectangle(Rectangle rectangle, Color color);

        /// <summary>
        /// Set a rectangle region we can render on.
        /// Any rendering that exceed this region, would be culled out.
        /// </summary>
        /// <param name="region">Visible region. Only pixels within this rectangle will be rendered.</param>
        void SetScissorRegion(Rectangle region);

        /// <summary>
        /// Get current scissor region, if set.
        /// </summary>
        /// <returns>Current scissor region or null if not set.</returns>
        Rectangle? GetScissorRegion();

        /// <summary>
        /// Clear scissor region, if set.
        /// </summary>
        void ClearScissorRegion();
    }
}
