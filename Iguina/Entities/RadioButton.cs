using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Radio button entity type.
    /// </summary>
    public class RadioButton : CheckedEntity
    {
        /// <summary>
        /// Radio button label.
        /// </summary>
        public Paragraph Paragraph { get; private set; }

        /// <inheritdoc/>
        internal override bool Interactable => true;


        /// <summary>
        /// Create the radio button.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Radio button stylesheet.</param>
        /// <param name="text">Radio button text.</param>
        public RadioButton(UISystem system, StyleSheet? stylesheet, string text = "New Radio Button") : base(system, stylesheet)
        {
            // create the radio button paragraph
            Paragraph = new Paragraph(system, stylesheet, text);
            Paragraph.DrawFillTexture = false;
            AddChildInternal(Paragraph);
            Paragraph.CopyStateFrom = this;

            // make checkable
            ToggleCheckOnClick = true;
            CanClickToUncheck = false;
            ExclusiveSelection = true;
        }

        /// <summary>
        /// Create the radio button with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Radio button text.</param>
        public RadioButton(UISystem system, string text = "New Radio Button") : this(system, 
            system.DefaultStylesheets.RadioButtons ?? system.DefaultStylesheets.CheckBoxes, 
            text)
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
