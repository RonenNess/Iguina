using Iguina.Defs;

namespace Iguina.Entities
{
    /// <summary>
    /// Interface for an entity that is used to pick color.
    /// </summary>
    public interface IColorPicker
    {
        /// <summary>
        /// Get / set the picker color value.
        /// </summary>
        Color ColorValue { get; set; }

        /// <summary>
        /// Set the color value of this picker from a given color.
        /// If color is not found in source texture, it will set to the nearest found value.
        /// </summary>
        /// <param name="value">Color value to set to.</param>
        void SetColorValueApproximate(Color value);
    }
}
