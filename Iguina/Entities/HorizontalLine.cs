using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A graphical horizontal line that separates between entities.
    /// </summary>
    public class HorizontalLine : Entity
    {
        /// <summary>
        /// Create the horizontal line.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Horizontal line stylesheet.</param>
        public HorizontalLine(UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
        {
        }

        /// <summary>
        /// Create the horizontal line with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public HorizontalLine(UISystem system) : this(system, system.DefaultStylesheets.HorizontalLines)
        {
        }

        /// <inheritdoc/>
        protected override Anchor GetDefaultEntityTypeAnchor()
        {
            return Anchor.AutoCenter;
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPercents(100f);
            ret.Y.SetPixels(8);
            return ret;
        }
    }
}
