using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A label text entity.
    /// Same as a paragraph, but with different defaults values and stylesheet.
    /// </summary>
    public class Label : Paragraph
    {
        /// <summary>
        /// Create the label.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Label stylesheet.</param>
        /// <param name="text">Label text.</param>
        /// <param name="ignoreInteractions">If true, this label will ignore user interactions.</param>
        public Label(UISystem system, StyleSheet? stylesheet, string text = "New Label", bool ignoreInteractions = true) : base(system, stylesheet, text, ignoreInteractions)
        {
        }

        /// <summary>
        /// Create the label with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Label text.</param>
        /// <param name="ignoreInteractions">If true, this label will ignore user interactions.</param>
        public Label(UISystem system, string text = "New Label", bool ignoreInteractions = true) : this(system, system.DefaultStylesheets.Labels, text, ignoreInteractions)
        {
        }
    }
}
