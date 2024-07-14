using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A title text entity.
    /// Same as a paragraph, but with different defaults values and stylesheet.
    /// </summary>
    public class Title : Paragraph
    {
        /// <summary>
        /// Create the title.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Title stylesheet.</param>
        /// <param name="text">Title text.</param>
        public Title(UISystem system, StyleSheet? stylesheet, string text = "New Title") : base(system, stylesheet, text)
        {
        }

        /// <summary>
        /// Create the title with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Title text.</param>
        public Title(UISystem system, string text = "New Title") : this(system, system.DefaultStylesheets.Titles, text)
        {
        }
    }
}
