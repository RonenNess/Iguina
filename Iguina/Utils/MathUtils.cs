
namespace Iguina.Utils
{
    /// <summary>
    /// Math utils.
    /// </summary>
    internal static class MathUtils
    {
        /// <summary>
        /// Lerp numeric value.
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
