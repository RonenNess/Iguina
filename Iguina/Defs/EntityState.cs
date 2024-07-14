

namespace Iguina.Defs
{
    /// <summary>
    /// Different states an entity can be in.
    /// </summary>
    public enum EntityState
    {
        /// <summary>
        /// Default inactive state.
        /// </summary>
        Default,

        /// <summary>
        /// Entity is currently being targeted. For example, the mouse points on the entity.
        /// </summary>
        Targeted,

        /// <summary>
        /// Entity is currently being interacted with, for example text input entity we're currently typing into, or a button that is being pressed.
        /// </summary>
        Interacted,

        /// <summary>
        /// Entity is checked. For checkbox, radio button, or other entities that have a 'checked' state.
        /// </summary>
        Checked,

        /// <summary>
        /// Entity is currently being targeted, and also checked. For example, the mouse points on the entity.
        /// </summary>
        TargetedChecked,

        /// <summary>
        /// Entity is disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// Entity is disabled but also checked.
        /// </summary>
        DisabledChecked,
    }
}
