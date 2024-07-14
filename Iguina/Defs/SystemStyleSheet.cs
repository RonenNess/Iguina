

namespace Iguina.Defs
{
    /// <summary>
    /// System-level stylesheet for UI.
    /// This define general properties that are not related to any specific entity.
    /// </summary>
    public class SystemStyleSheet
    {
        /// <summary>
        /// UI Theme identifier.
        /// </summary>
        public string ThemeIdentifier { get; set; } = null!;

        /// <summary>
        /// Cursor to render in default state.
        /// </summary>
        public CursorProperties? CursorDefault { get; set; }

        /// <summary>
        /// Cursor to render while pointing on an interactable entity that is not locked or disabled.
        /// </summary>
        public CursorProperties? CursorInteractable { get; set; }

        /// <summary>
        /// Cursor to render while pointing on a disabled entity.
        /// </summary>
        public CursorProperties? CursorDisabled { get; set; }

        /// <summary>
        /// Cursor to render while pointing on a locked entity.
        /// </summary>
        public CursorProperties? CursorLocked { get; set; }

        /// <summary>
        /// Lock entities to interactive state for at least this value in seconds, to make sure the 'interactive' state is properly displayed even for rapid clicks.
        /// </summary>
        public float TimeToLockInteractiveState { get; set; }

        /// <summary>
        /// Default stylesheets to load for entities.
        /// Key = name of stylesheet to load (for example 'Panels' for 'uiSystem.DefaultStylesheets.Panels'.
        /// Value = path, relative to the folder containing this stylesheet, to load from.
        /// </summary>
        public Dictionary<string, string> LoadDefaultStylesheets { get; set; } = null!;
    }
}
