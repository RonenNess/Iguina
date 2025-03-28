

namespace Iguina.Defs
{
    /// <summary>
    /// Define a texture with internal source rect and frame source rect.
    /// When rendered on a region, it will tile-render the frame parts on the edges, and the center part repeating in the fill.
    /// </summary>
    public class FramedTexture
    {
        /// <summary>
        /// Texture identifier.
        /// </summary>
        public string TextureId { get; set; } = null!;

        /// <summary>
        /// The source rectangle of the center part, without the frame.
        /// </summary>
        public Rectangle InternalSourceRect 
        {
            get
            {
                if (_internalSourceRect.Width == 0 && _internalSourceRect.Height == 0 && FrameWidth.HasValue)
                {
                    _internalSourceRect = new Rectangle(
                        ExternalSourceRect.X + FrameWidth.Value.X,
                        ExternalSourceRect.Y + FrameWidth.Value.Y,
                        ExternalSourceRect.Width - FrameWidth.Value.X * 2,
                        ExternalSourceRect.Height - FrameWidth.Value.Y * 2);
                }
                return _internalSourceRect;
            }
            set => _internalSourceRect = value;
        }

        // internal source rect value
        Rectangle _internalSourceRect;

        /// <summary>
        /// Graphics frame width.
        /// If set, it will set the value of InternalSourceRect by calculating ExternalSourceRect - FrameWidth.
        /// </summary>
        public Point? FrameWidth
        {
            get => _frameWidth;
            set
            {
                _frameWidth = value;
                if (value.HasValue)
                {
                    _internalSourceRect = new Rectangle();
                }
            }
        }

        // frame width value
        Point? _frameWidth;

        /// <summary>
        /// The source rectangle of the entire framed texture, including the frame.
        /// </summary>
        public Rectangle ExternalSourceRect { get; set; }

        /// <summary>
        /// Get the source rectangle of the top frame, without corners.
        /// </summary>
        public Rectangle TopSourceRect => new Rectangle(InternalSourceRect.Left, ExternalSourceRect.Top, InternalSourceRect.Width, InternalSourceRect.Top - ExternalSourceRect.Top);

        /// <summary>
        /// Get the source rectangle of the bottom frame, without corners.
        /// </summary>
        public Rectangle BottomSourceRect => new Rectangle(InternalSourceRect.Left, InternalSourceRect.Bottom, InternalSourceRect.Width, ExternalSourceRect.Bottom - InternalSourceRect.Bottom);

        /// <summary>
        /// Get the source rectangle of the left frame, without corners.
        /// </summary>
        public Rectangle LeftSourceRect => new Rectangle(ExternalSourceRect.Left, InternalSourceRect.Top, InternalSourceRect.Left - ExternalSourceRect.Left, InternalSourceRect.Height);

        /// <summary>
        /// Get the source rectangle of the right frame, without corners.
        /// </summary>
        public Rectangle RightSourceRect => new Rectangle(InternalSourceRect.Right, InternalSourceRect.Top, ExternalSourceRect.Right - InternalSourceRect.Right, InternalSourceRect.Height);

        /// <summary>
        /// Get the source rectangle of the top left corner.
        /// </summary>
        public Rectangle TopLeftSourceRect => new Rectangle(ExternalSourceRect.Left, ExternalSourceRect.Top, InternalSourceRect.Left - ExternalSourceRect.Left, InternalSourceRect.Top - ExternalSourceRect.Top);

        /// <summary>
        /// Get the source rectangle of the top right corner.
        /// </summary>
        public Rectangle TopRightSourceRect => new Rectangle(InternalSourceRect.Right, ExternalSourceRect.Top, ExternalSourceRect.Right - InternalSourceRect.Right, InternalSourceRect.Top - ExternalSourceRect.Top);

        /// <summary>
        /// Get the source rectangle of the bottom left corner.
        /// </summary>
        public Rectangle BottomLeftSourceRect => new Rectangle(ExternalSourceRect.Left, InternalSourceRect.Bottom, InternalSourceRect.Left - ExternalSourceRect.Left, ExternalSourceRect.Bottom - InternalSourceRect.Bottom);

        /// <summary>
        /// Get the source rectangle of the bottom right corner.
        /// </summary>
        public Rectangle BottomRightSourceRect => new Rectangle(InternalSourceRect.Right, InternalSourceRect.Bottom, ExternalSourceRect.Right - InternalSourceRect.Right, ExternalSourceRect.Bottom - InternalSourceRect.Bottom);

        /// <summary>
        /// Will scale frame and source parts by this factor.
        /// </summary>
        public float TextureScale { get; set; } = 1f;

        /// <summary>
        /// Offset in pixels.
        /// </summary>
        public Point Offset { get; set; }
    }
}
