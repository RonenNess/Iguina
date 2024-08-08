using Iguina.Defs;
using Iguina.Utils;

namespace Iguina.Entities
{
    /// <summary>
    /// Progress bars are like sliders, but instead of moving a handle they 'fill' an internal entity.
    /// These entities are useful to show progress, health bars, etc.
    /// </summary>
    /// <remarks>By default, progress bars have 'IgnoreInteractions' set to true. If you want the progress bar to behave like a slider and allow users to change its value, set it to false.</remarks>
    public class ProgressBar : Slider
    {
        /// <summary>
        /// Create the progress bar.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Progress bar stylesheet.</param>
        /// <param name="fillStylesheet">Progress bar fill stylesheet.</param>
        /// <param name="orientation">Progress bar orientation.</param>
        public ProgressBar(UISystem system, StyleSheet? stylesheet, StyleSheet? fillStylesheet, Orientation orientation = Orientation.Horizontal) : base(system, stylesheet, fillStylesheet, orientation)
        {
            Handle.IgnoreInteractions = IgnoreInteractions = true;
            Handle.Anchor = Handle.StyleSheet.DefaultAnchor ?? stylesheet?.DefaultAnchor ?? ((orientation == Orientation.Horizontal) ? Anchor.CenterLeft : Anchor.TopCenter);
        }

        /// <summary>
        /// Create the progress bar with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="orientation">Progress bar orientation.</param>
        public ProgressBar(UISystem system, Orientation orientation = Orientation.Horizontal) :
            this(system,
                (orientation == Orientation.Horizontal) ? system.DefaultStylesheets.HorizontalProgressBars : system.DefaultStylesheets.VerticalProgressBars,
                (orientation == Orientation.Horizontal) ? system.DefaultStylesheets.HorizontalProgressBarsFill : system.DefaultStylesheets.VerticalProgressBarsFill,
                orientation)
        {
        }

        /// <inheritdoc/>
        protected override void Update(float dt)
        {
            base.Update(dt);
            Handle.IgnoreInteractions = IgnoreInteractions;
        }

        /// <inheritdoc/>
        protected override void UpdateHandle(float dt)
        {
            var valuePercent = ValuePercent;
            if (Orientation == Orientation.Horizontal)
            {
                if (InterpolateHandlePosition)
                {
                    var currValue = Handle.Size.X.Value;
                    Handle.Size.X.SetPercents(MathUtils.Lerp(currValue, valuePercent * 100f, dt * HandleInterpolationSpeed));
                }
                else
                {
                    Handle.Size.X.SetPercents(valuePercent * 100f);
                }
            }
            else
            {
                if (InterpolateHandlePosition)
                {
                    var currValue = Handle.Size.Y.Value;
                    Handle.Size.Y.SetPercents(MathUtils.Lerp(currValue, valuePercent * 100f, dt * HandleInterpolationSpeed));
                }
                else
                {
                    Handle.Size.Y.SetPercents(valuePercent * 100f);
                }
            }
        }
    }
}
