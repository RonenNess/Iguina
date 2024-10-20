﻿
namespace Iguina.Defs
{
    /// <summary>
    /// A texture to render as icon, with size based on source rectangle.
    /// </summary>
    public class IconTexture
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
        /// Will scale icon by this factor.
        /// </summary>
        public float TextureScale { get; set; } = 1f;

        /// <summary>
        /// If true, will center icon horizontally.
        /// </summary>
        public bool CenterHorizontally { get; set; }

        /// <summary>
        /// If true, will center icon vertically.
        /// </summary>
        public bool CenterVertically { get; set; }

        /// <summary>
        /// Icon offset in pixels.
        /// </summary>
        public Point Offset { get; set; }
    }
}
