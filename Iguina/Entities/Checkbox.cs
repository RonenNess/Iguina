using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Checkbox entity type.
    /// </summary>
    public class Checkbox : CheckedEntity
    {
        /// <summary>
        /// Checkbox label.
        /// </summary>
        public Paragraph Paragraph { get; private set; }

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <summary>
        /// Create the checkbox.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Checkbox stylesheet.</param>
        /// <param name="text">Checkbox text.</param>
        public Checkbox(UISystem system, StyleSheet? stylesheet, string text = "New Checkbox") : base(system, stylesheet)
        {
            // create the checkbox paragraph
            Paragraph = new Paragraph(system, stylesheet, text);
            Paragraph.DrawFillTexture = false;
            AddChildInternal(Paragraph);
            Paragraph.CopyStateFrom = this;

            // make checkable
            ToggleCheckOnClick = true;
            CanClickToUncheck = true;

        }

        /// <summary>
        /// Create the checkbox with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Checkbox text.</param>
        public Checkbox(UISystem system, string text = "New Button") : this(system, system.DefaultStylesheets.CheckBoxes, text)
        {
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPercents(100f);
            ret.Y.SetPixels(54);
            return ret;
        }
    }
}
