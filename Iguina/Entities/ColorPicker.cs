using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Color picker lets the user pick a color from a 2d source texture, with X and Y axes.
    /// This is useful for color pickers that have a combination of hue and brightness.
    /// </summary>
    public class ColorPicker : Entity
    {
        /// <summary>
        /// The entity used as the color picker handle.
        /// </summary>
        public Entity Handle { get; private set; }

        // last handle offset
        Point _lastHandleOffset = new Point(0, 0);

        /// <summary>
        /// Get color picker value, as a color extracted from the source texture.
        /// </summary>
        public Color ColorValue => UISystem.Renderer.GetPixelFromTexture(StyleSheet.Default?.FillTextureStretched?.TextureId ?? string.Empty, GetOffsetInTexture());

        /// <summary>
        /// Get source stretch texture.
        /// </summary>
        StretchedTexture? SourceTextureData => StyleSheet.GetProperty<StretchedTexture>("FillTextureStretched", State, null, OverrideStyles);

        /// <summary>
        /// Get offset in texture based on current offset.
        /// </summary>
        Point GetOffsetInTexture()
        {
            var srcTexture = SourceTextureData;
            var srcX = (srcTexture?.SourceRect.X ?? 1f);
            var srcY = (srcTexture?.SourceRect.Y ?? 1f);
            var srcWidth = (srcTexture?.SourceRect.Width ?? 1f);
            var srcHeight = (srcTexture?.SourceRect.Height ?? 1f);
            float factorX = srcWidth / (float)Math.Max(1, LastBoundingRect.Width);
            float factorY = srcHeight / (float)Math.Max(1, LastBoundingRect.Height);
            return new Point(
                (int)Math.Floor(srcX + _lastHandleOffset.X * factorX),
                (int)Math.Floor(srcY + _lastHandleOffset.Y * factorY)
            );
        }

        /// <summary>
        /// Create the color picker.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Color picker stylesheet.</param>
        /// <param name="stylesheet">Color picker handle stylesheet.</param>
        public ColorPicker(UISystem system, StyleSheet? stylesheet, StyleSheet? handleStylesheet) : base(system, stylesheet)
        {
            // create handle
            Handle = new Entity(system, handleStylesheet);
            Handle.CopyStateFrom = this;
            Handle.Anchor = Anchor.TopLeft;
            Handle.TransferInteractionsTo = this;
            AddChildInternal(Handle);
            Handle.IgnoreScrollOffset = true;
        }

        /// <summary>
        /// Create the color picker with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public ColorPicker(UISystem system) :
            this(system,
                system.DefaultStylesheets.ColorPickers,
                system.DefaultStylesheets.ColorPickersHandle)
        {
        }

        /// <inheritdoc/>
        protected override void Update(float dt)
        {
            base.Update(dt);

            int offsetX = (int)Handle.Offset.X.Value + Handle.LastBoundingRect.Width / 2;
            int offsetY = (int)Handle.Offset.Y.Value + Handle.LastBoundingRect.Height / 2;
            if ((_lastHandleOffset.X != offsetX) || (_lastHandleOffset.Y != offsetY))
            {
                _lastHandleOffset.X = offsetX;
                _lastHandleOffset.Y = offsetY;
                Events.OnValueChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Set the color picker handle offset.
        /// </summary>
        /// <param name="offset">Color picker offset, in pixels, from top-left corner.</param>
        public void SetHandleOffset(Point offset)
        {
            Handle.Offset.X.Value = offset.X - Handle.LastBoundingRect.Width / 2;
            Handle.Offset.Y.Value = offset.Y - Handle.LastBoundingRect.Height / 2;
        }

        /// <inheritdoc/>
        internal override void DoInteractions(InputState inputState)
        {
            // do base interactions
            base.DoInteractions(inputState);

            // select value via mouse
            if (inputState.LeftMouseDown)
            {
                SetHandleOffset(new Point(inputState.MousePosition.X - LastBoundingRect.X, inputState.MousePosition.Y - LastBoundingRect.Y));
            }
        }
    }
}
