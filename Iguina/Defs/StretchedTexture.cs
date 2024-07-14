

namespace Iguina.Defs
{
    /// <summary>
    /// A texture to render stretched over the given region.
    /// </summary>
    public class StretchedTexture
    {
        /// <summary>
        /// Texture identifier.
        /// </summary>
        public string TextureId { get; set; } = null!;

        /// <summary>
        /// The source rectangle of the texture to draw.
        /// </summary>
        public Rectangle SourceRect { get; set; }

        /// <summary>
        /// Add extra size to the sides of this texture when rendering it.
        /// </summary>
        public Sides? ExtraSize { get; set; }
    }
}
