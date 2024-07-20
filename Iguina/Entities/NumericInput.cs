using Iguina.Defs;
using System.Globalization;


namespace Iguina.Entities
{
    /// <summary>
    /// Text input that accept only numbers, and have a - and + buttons.
    /// </summary>
    public class NumericInput : TextInput
    {
        /// <inheritdoc/>
        public override bool Multiline
        {
            get => false;
            set => throw new InvalidOperationException("Numeric input fields can't be multiline!");
        }

        /// <inheritdoc/>
        public override string? PlaceholderText
        {
            get => DefaultValue.ToString();
            set => throw new InvalidOperationException("Numeric input fields can't have placeholder text!");
        }

        /// <inheritdoc/>
        public override string Value
        {
            get => base.Value;
            set
            {
                // remove decimal if not accepted
                if (!AcceptsDecimal)
                {
                    value = value.Split(DecimalSeparator)[0];
                }

                // trim
                value = value.Trim();

                // if empty or a single decimal separator, stop here and set to empty
                if ((value.Length == 0) || (value.Length == 1 && value[0] == DecimalSeparator))
                {
                    base.Value = string.Empty;
                    _valueFloat = null;
                    return;
                }

                // special case: if the only input is - it might be the begining of a negative number, so we allow it
                if ((MinValue == null || MinValue.Value < 0f) && (value == "-"))
                {
                    base.Value = value;
                    _valueFloat = null;
                    return;
                }

                // set float value and base value
                if (float.TryParse(value, CultureInfo, out float result))
                {
                    // check min value
                    if (result < MinValue)
                    {
                        result = MinValue.Value;
                        value = result.ToString();
                    }

                    // check max value
                    if (result > MaxValue)
                    {
                        result = MaxValue.Value;
                        value = result.ToString();
                    }

                    // special - if value is 0, make sure input is not 00000...)
                    if (result == 0f)
                    {
                        value = "0";
                    }
                    // if not 0, trim zeroes from the start
                    else if (value.StartsWith('0'))
                    {
                        value = value.TrimStart('0');
                    }

                    // set value
                    base.Value = value;
                    _valueFloat = result;
                }

                // failed to parse? don't change value!
            }
        }


        /// <summary>
        /// Optional min value.
        /// </summary>
        public float? MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    if (_maxValue <= _minValue) { throw new ArgumentOutOfRangeException("Numeric Input min value must be smaller than max value!"); }
                }
            }
        }
        float? _minValue;

        /// <summary>
        /// Optional max value.
        /// </summary>
        public float? MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    if (_maxValue <= _minValue) { throw new ArgumentOutOfRangeException("Numeric Input max value must be bigger than min value!"); }
                }
            }
        }
        float? _maxValue;

        /// <summary>
        /// Get / set value as a float number.
        /// </summary>
        public float NumericValue
        {
            get => _valueFloat ?? DefaultValue;
            set
            {
                if (!AcceptsDecimal && (MathF.Floor(value) != value))
                {
                    throw new InvalidOperationException("Can't set NumericInput float value while not accepting decimal point!");
                }

                if (value < MinValue) { throw new ArgumentOutOfRangeException("Numeric Input value can't be smaller than min value!"); }
                if (value > MaxValue) { throw new ArgumentOutOfRangeException("Numeric Input value can't be bigger than max value!"); }

                Value = value.ToString(CultureInfo);
            }
        }

        // current value as float
        float? _valueFloat = null;

        /// <summary>
        /// Default value to return when there's no numeric input or when an invalid value is provided.
        /// </summary>
        public float DefaultValue = 0f;

        /// <summary>
        /// Culture info to use when parsing floats.
        /// This will affect the decimal point character.
        /// </summary>
        public CultureInfo CultureInfo = CultureInfo.InvariantCulture;

        /// <summary>
        /// Return the character used as decimal point.
        /// </summary>
        public char DecimalSeparator => CultureInfo.NumberFormat.NumberDecimalSeparator[0];

        /// <summary>
        /// If true, this numeric input accepts a decimal point.
        /// If false it will not support it.
        /// </summary>
        public bool AcceptsDecimal = true;

        /// <summary>
        /// Create the numeric text input.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Numeric input stylesheet.</param>
        public NumericInput(UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
        {
        }

        /// <summary>
        /// Create the numeric text input with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public NumericInput(UISystem system) : this(system, system.DefaultStylesheets.NumericTextInput ?? system.DefaultStylesheets.TextInput)
        {
        }
    }
}
