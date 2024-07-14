using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Button entity type.
    /// </summary>
    public class Button : CheckedEntity
    {
        /// <summary>
        /// Button label.
        /// </summary>
        public Paragraph Paragraph { get; private set; }

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <summary>
        /// Create the button.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Button stylesheet.</param>
        /// <param name="text">Button text.</param>
        public Button(UISystem system, StyleSheet? stylesheet, string text = "New Button") : base(system, stylesheet)
        {
            // create the button paragraph
            Paragraph = new Paragraph(system, stylesheet, text);
            Paragraph.DrawFillTexture = false;
            AddChildInternal(Paragraph);
            Paragraph.CopyStateFrom = this;
            OverflowMode = OverflowMode.HideOverflow;
        }

        /// <summary>
        /// Create the button with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Button text.</param>
        public Button(UISystem system, string text = "New Button") : this(system, system.DefaultStylesheets.Buttons, text)
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
