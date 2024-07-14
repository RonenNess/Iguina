
namespace Iguina.Defs
{
    /// <summary>
    /// Define how to handle child entities that go out of the entity's bounds.
    /// </summary>
    public enum OverflowMode
    {
        /// <summary>
        /// Child entities can overflow bounding rectangle freely.
        /// </summary>
        AllowOverflow,

        /// <summary>
        /// Child entities rendering will be cut off if they go out of bounding rectangle.
        /// </summary>
        HideOverflow,
    }
}
