using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Color slider is a color picker entity that is based on slider, where the user can pick a color from a range.
    /// This picker is useful for when the user can pick hue without choosing brightness or saturation.
    /// </summary>
    public class ColorSlider : Slider
    {
        /// <summary>
        /// Get slider color value, based on the default state source texture.
        /// </summary>
        public Color ColorValue
        {
            get
            {
                var srcRect = SourceRectangle;
                if (Orientation == Orientation.Horizontal)
                {
                    return UISystem.Renderer.GetPixelFromTexture(SourceTextureId, new Point(srcRect.Left + (int)Math.Floor(ValuePercent * (srcRect.Width - 0.1f)), srcRect.Top + srcRect.Height / 2));
                }
                else
                {
                    return UISystem.Renderer.GetPixelFromTexture(SourceTextureId, new Point(srcRect.Left + srcRect.Width / 2, srcRect.Top + (int)Math.Floor(ValuePercent * (srcRect.Height - 0.1f))));
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

        /// <summary>
        /// Get slider source rectangle.
        /// </summary>
        Rectangle SourceRectangle => StyleSheet?.Default?.FillTextureStretched?.SourceRect ?? StyleSheet?.Default?.Icon?.SourceRect ?? Rectangle.Empty;

        /// <summary>
        /// Get slider source texture.
        /// </summary>
        string SourceTextureId => StyleSheet?.Default?.FillTextureStretched?.TextureId ?? StyleSheet?.Default?.Icon?.TextureId ?? string.Empty;

        /// <inheritdoc/>
        protected override void SetAutoRange()
        {
            MinValue = 0;
            if (Orientation == Orientation.Horizontal)
            {
                MaxValue = Math.Max(StyleSheet?.Default?.FillTextureStretched?.SourceRect.Width ?? StyleSheet?.Default?.Icon?.SourceRect.Width ?? LastBoundingRect.Width, 10);
            }
            else
            {
                MaxValue = Math.Max(StyleSheet?.Default?.FillTextureStretched?.SourceRect.Height ?? StyleSheet?.Default?.Icon?.SourceRect.Height ?? LastBoundingRect.Height, 10);
            }
            StepsCount = (uint)MaxValue;
        }
    }
}
