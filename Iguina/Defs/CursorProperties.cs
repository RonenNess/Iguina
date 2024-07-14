

namespace Iguina.Defs
{
    /// <summary>
    /// Describe how to render the cursor.
    /// </summary>
    public class CursorProperties
    {
        /// <summary>
        /// Texture identifier.
        /// </summary>
        public string TextureId { get; set; } = null!;

        /// <summary>
        /// The source rectangle of the cursor texture to draw.
        /// An empty rectangle (0, 0, 0, 0) will render the entire texture.
        /// </summary>
        public Rectangle SourceRect { get; set; }

        /// <summary>
        /// Fill color tint.
        /// </summary>
        public Color FillColor { get; set; } = new Color(255, 255, 255, 255);

        /// <summary>
        /// Effect to use while rendering the cursor.
        /// This can be used by the host application as actual shader name, a flag identifier to change rendering, or any other purpose.
        /// </summary>
        public string EffectIdentifier { get; set; } = null!;

        /// <summary>
        /// Cursor graphics offset from top-left corner, in pixels.
        /// </summary>
        public Point Offset { get; set; }

        /// <summary>
        /// Cursor scale.
        /// </summary>
        public float Scale { get; set; } = 1f;
    }
}
