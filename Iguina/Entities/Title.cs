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
        /// <param name="ignoreInteractions">If true, this title will ignore user interactions.</param>
        public Title(UISystem system, StyleSheet? stylesheet, string text = "New Title", bool ignoreInteractions = true) : base(system, stylesheet, text, ignoreInteractions)
        {
        }

        /// <summary>
        /// Create the title with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="text">Title text.</param>
        /// <param name="ignoreInteractions">If true, this title will ignore user interactions.</param>
        public Title(UISystem system, string text = "New Title", bool ignoreInteractions = true) : this(system, system.DefaultStylesheets.Titles, text, ignoreInteractions)
        {
        }
    }
}
