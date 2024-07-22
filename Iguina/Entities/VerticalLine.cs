using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A graphical vertical line that separates between entities.
    /// </summary>
    public class VerticalLine : Entity
    {
        /// <summary>
        /// Create the vertical line.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Vertical line stylesheet.</param>
        public VerticalLine(UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
        {
            IgnoreInteractions = true;
        }

        /// <summary>
        /// Create the vertical line with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public VerticalLine(UISystem system) : this(system, system.DefaultStylesheets.VerticalLines)
        {
        }

        /// <inheritdoc/>
        protected override Anchor GetDefaultEntityTypeAnchor()
        {
            return Anchor.AutoInlineLTR;
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPixels(8);
            ret.Y.SetPercents(100f);
            return ret;
        }
    }
}
