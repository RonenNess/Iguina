using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Color slider is a color picker entity that is based on slider, where the user can pick a color from a range.
    /// This picker is useful for when the user can pick hue without choosing brightness or saturation.
    /// </summary>
    public class ColorSlider : Slider, IColorPicker
    {
        /// <summary>
        /// Get / set slider color value, based on the default state source texture.
        /// </summary>
        public Color ColorValue
        {
            get
            {
                var srcRect = SourceRectangle;
                if (Orientation == Orientation.Horizontal)
                {
                    return UISystem.Renderer.GetPixelFromTexture(SourceTextureId, new Point(srcRect.Left + (int)Math.Round(ValuePercent * (srcRect.Width - 0.1f)), srcRect.Top + srcRect.Height / 2));
                }
                else
                {
                    return UISystem.Renderer.GetPixelFromTexture(SourceTextureId, new Point(srcRect.Left + srcRect.Width / 2, srcRect.Top + (int)Math.Round(ValuePercent * (srcRect.Height - 0.1f))));
                }
            }
            set
            {
                var src = SourceRectangle;
                src.Y += src.Height / 2;
                src.Height = 1;
                var offset = UISystem.Renderer.FindPixelOffsetInTexture(SourceTextureId, SourceRectangle, value, false);
                if (offset.HasValue)
                {
                    if (Orientation == Orientation.Horizontal)
                    {
                        ValueSafe = offset.Value.X;
                    }
                    else
                    {
                        ValueSafe = offset.Value.Y;
                    }
                }
            }
        }

        /// <summary>
        /// Create the colors slider.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Slider stylesheet.</param>
        /// <param name="stylesheet">Slider handle stylesheet.</param>
        /// <param name="orientation">Slider orientation.</param>
        public ColorSlider(UISystem system, StyleSheet? stylesheet, StyleSheet? handleStylesheet, Orientation orientation = Orientation.Horizontal) : base(system, stylesheet, handleStylesheet, orientation)
        {
            AutoSetRange = true;
            SetAutoRange();
        }

        /// <summary>
        /// Create the color slider with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="orientation">Slider orientation.</param>
        public ColorSlider(UISystem system, Orientation orientation = Orientation.Horizontal) :
            this(system,
                (orientation == Orientation.Horizontal) ? (system.DefaultStylesheets.HorizontalColorSliders ?? system.DefaultStylesheets.HorizontalSliders) : (system.DefaultStylesheets.VerticalColorSliders ?? system.DefaultStylesheets.VerticalSliders),
                (orientation == Orientation.Horizontal) ? (system.DefaultStylesheets.HorizontalColorSlidersHandle ?? system.DefaultStylesheets.HorizontalSlidersHandle) : (system.DefaultStylesheets.VerticalColorSlidersHandle ?? system.DefaultStylesheets.VerticalSlidersHandle),
                orientation)
        {
        }

        /// <inheritdoc/>
        public void SetColorValueApproximate(Color value)
        {
            var src = SourceRectangle;
            src.Y += src.Height / 2;
            src.Height = 1;
            var offset = UISystem.Renderer.FindPixelOffsetInTexture(SourceTextureId, SourceRectangle, value, true);
            if (offset.HasValue)
            {
                if (Orientation == Orientation.Horizontal)
                {
                    ValueSafe = offset.Value.X;
                }
                else
                {
                    ValueSafe = offset.Value.Y;
                }
            }
        }

        /// <summary>
        /// Get source stretch texture.
        /// </summary>
        StretchedTexture? SourceTextureData => StyleSheet.GetProperty<StretchedTexture>("FillTextureStretched", State, null, OverrideStyles);

        /// <summary>
        /// Get slider source rectangle.
        /// </summary>
        Rectangle SourceRectangle => SourceTextureData?.SourceRect ?? Rectangle.Empty;

        /// <summary>
        /// Get slider source texture.
        /// </summary>
        string SourceTextureId => SourceTextureData?.TextureId ?? string.Empty;

        /// <inheritdoc/>
        protected override void SetAutoRange()
        {
            MinValue = 0;
            if (Orientation == Orientation.Horizontal)
            {
                MaxValue = Math.Max(SourceTextureData?.SourceRect.Width ?? LastBoundingRect.Width, 10);
            }
            else
            {
                MaxValue = Math.Max(SourceTextureData?.SourceRect.Height ?? LastBoundingRect.Height, 10);
            }
            StepsCount = (uint)MaxValue;
        }
    }
}
