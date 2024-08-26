using System.Text.Json.Serialization;


namespace Iguina.Defs
{
    /// <summary>
    /// Define a UI measurement unit.
    /// For example, position, size, etc.
    /// </summary>
    public struct Measurement
    {
        /// <summary>
        /// Measure value.
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Measure unit type.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MeasureUnit Units { get; set; }

        /// <summary>
        /// Set value as pixels.
        /// </summary>
        public void SetPixels(int value)
        {
            Value = (float)value;
            Units = MeasureUnit.Pixels;
        }

        /// <summary>
        /// Set value as percents.
        /// </summary>
        public void SetPercents(float percent)
        {
            Value = Math.Max(0f, percent);
            Units = MeasureUnit.PercentOfParent;
        }

        /// <summary>
        /// Get value in pixels.
        /// </summary>
        /// <param name="parentSize">Relevant parent size.</param>
        /// <returns>Measurement value in pixels.</returns>
        public int GetValueInPixels(int parentSize)
        {
            if (Units == MeasureUnit.Pixels) { return (int)Value; }
            return (int)MathF.Round(parentSize * (Value / 100f));
        }
    }

    /// <summary>
    /// GUI measure unit with X and Y components.
    /// </summary>
    public struct MeasureVector
    {
        /// <summary>
        /// Measure unit on X axis.
        /// </summary>
        public Measurement X;

        /// <summary>
        /// Measure unit on Y axis.
        /// </summary>
        public Measurement Y;

        /// <summary>
        /// Create and return measure vector with pixel values.
        /// </summary>
        public static MeasureVector FromPixels(int x, int y)
        {
            var ret = new MeasureVector();
            ret.SetPixels(x, y);
            return ret;
        }

        /// <summary>
        /// Create and return measure vector with float values.
        /// </summary>
        public static MeasureVector FromPercents(float x, float y)
        {
            var ret = new MeasureVector();
            ret.SetPercents(x, y);
            return ret;
        }

        /// <summary>
        /// Set value as pixels.
        /// </summary>
        public void SetPixels(int x, int y)
        {
            X.SetPixels(x);
            Y.SetPixels(y);
        }

        /// <summary>
        /// Set value as percent of parent size.
        /// </summary>
        public void SetPercents(float x, float y)
        {
            X.SetPercents(x);
            Y.SetPercents(y);
        }
    }

    /// <summary>
    /// Measure unit types.
    /// </summary>
    public enum MeasureUnit
    {
        /// <summary>
        /// Value is pixels.
        /// </summary>
        Pixels = 0,

        /// <summary>
        /// Value is percent of parent size.
        /// For example if the measure is talking about position x, it will be in perents of parent entity width.
        /// </summary>
        PercentOfParent = 1
    }
}
