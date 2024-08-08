using Iguina.Defs;
using System.Globalization;


namespace Iguina.Entities
{
    /// <summary>
    /// Text input that accepts only numbers, and has a - and + buttons.
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
            get => DefaultValue.ToString(CultureInfo);
            set => throw new InvalidOperationException("Numeric input fields can't have custom placeholder text!");
        }

        /// <summary>
        /// How much to increase / decrease values when clicking on the plus / minus buttons.
        /// </summary>
        public decimal ButtonsStepSize = 1;

        /// <inheritdoc/>
        public override string Value
        {
            get => base.Value;
            set
            {
                if (TryParseValue(value, out decimal? newValue, out string baseValue))
                {
                    _valueFloat = newValue;
                    base.Value = baseValue;
                }

                // If failed to parse, don't change current valid value!
            }
        }

        /// <inheritdoc/>
        internal override void PostUpdate(InputState inputState)
        {
            base.PostUpdate(inputState);

            if (inputState.LeftMouseDown)
            {
                if (_lockSelf && (_minusButton?.IsPointedOn(inputState.MousePosition) ?? false))
                {
                    _lockSelf = false;
                }

                if (_lockSelf && (_plusButton?.IsPointedOn(inputState.MousePosition) ?? false))
                {
                    _lockSelf = false;
                }
            }
        }

        /// <summary>
        /// Optional min value.
        /// </summary>
        public decimal? MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    if (_maxValue <= _minValue) { throw new ArgumentOutOfRangeException(nameof(value), "Numeric Input min value must be smaller than max value!"); }
                }
            }
        }
        decimal? _minValue;

        /// <summary>
        /// Optional max value.
        /// </summary>
        public decimal? MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    if (_maxValue <= _minValue) { throw new ArgumentOutOfRangeException(nameof(value), "Numeric Input max value must be bigger than min value!"); }
                }
            }
        }
        decimal? _maxValue;

        /// <summary>
        /// Get / set value as a float number.
        /// </summary>
        public decimal NumericValue
        {
            get => _valueFloat ?? DefaultValue;
            set
            {
                if (!AcceptsDecimal && (Math.Floor(value) != value))
                {
                    throw new InvalidOperationException("Can't set NumericInput float value while not accepting decimal point!");
                }

                if (value < MinValue) { throw new ArgumentOutOfRangeException(nameof(value), "Numeric Input value can't be smaller than min value!"); }
                if (value > MaxValue) { throw new ArgumentOutOfRangeException(nameof(value), "Numeric Input value can't be bigger than max value!"); }

                Value = value.ToString(CultureInfo);
            }
        }

        // current value as float
        decimal? _valueFloat = null;

        /// <summary>
        /// Default value to return when there's no numeric input or when an invalid value is provided.
        /// </summary>
        public decimal DefaultValue = 0;

        /// <summary>
        /// Culture info to use when parsing floats.
        /// This will affect the decimal point character.
        /// </summary>
        public CultureInfo CultureInfo
        {
            get => _cultureInfo;
            set
            {
                if (value.NumberFormat.NumberDecimalSeparator.Length > 1) throw new Exception("Culture must have a single decimal separator character");
                if (value.NumberFormat.NegativeSign.Length > 1) throw new Exception("Culture must have a single negative sign character");
                
                _cultureInfo = value;
            }
        }

        private CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

        /// <summary>
        /// Return the character used as decimal point.
        /// </summary>
        public char DecimalSeparator => CultureInfo.NumberFormat.NumberDecimalSeparator[0];

        /// <summary>
        /// Return the character used as the negative sign.
        /// </summary>
        public char NegativeSign => CultureInfo.NumberFormat.NegativeSign[0];

        /// <summary>
        /// If true, this numeric input accepts a decimal point.
        /// If false it will not support it.
        /// </summary>
        public bool AcceptsDecimal = true;

        // minus / plus buttons
        Button? _minusButton;
        Button? _plusButton;

        /// <summary>
        /// Set / get the minus button text.
        /// </summary>
        public string? MinusButtonText
        {
            get => _minusButton?.Paragraph?.Text ?? null;
            set { if (_minusButton != null) { _minusButton.Paragraph.Text = value ?? string.Empty; } }
        }

        /// <summary>
        /// Set / get the plus button text.
        /// </summary>
        public string? PlusButtonText
        {
            get => _plusButton?.Paragraph?.Text ?? null;
            set { if (_plusButton != null) { _plusButton.Paragraph.Text = value ?? string.Empty; } }
        }

        /// <summary>
        /// Create the numeric text input.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Numeric input stylesheet.</param>
        /// <param name="addPlusButton">If true, will add a plus button to increase value.</param>
        /// <param name="addMinusButton">If true, will add a minus button to decrease value.</param>
        public NumericInput(UISystem system, StyleSheet? stylesheet, bool addPlusButton = true, bool addMinusButton = true) : base(system, stylesheet)
        {
            var buttonStyle = system.DefaultStylesheets.NumericTextInputButton ?? system.DefaultStylesheets.Buttons;

            // add + button
            if (addPlusButton)
            {
                var plusButton = new Button(system, buttonStyle, "+");
                plusButton.Anchor = Anchor.CenterRight;
                plusButton.Offset.X.SetPixels(-GetPadding().Right + plusButton.GetMarginAfter().X);
                plusButton.Events.OnClick = (Entity entity) =>
                {
                    var value = NumericValue + ButtonsStepSize;
                    if (value < MinValue) { value = MinValue.Value; }
                    if (value > MaxValue) { value = MaxValue.Value; }
                    NumericValue = value;
                };
                AddChildInternal(plusButton);
                _plusButton = plusButton;
            }

            // add - button
            if (addMinusButton)
            {
                var minusButton = new Button(system, buttonStyle, "-");
                minusButton.Anchor = Anchor.CenterLeft;
                minusButton.Offset.X.SetPixels(-GetPadding().Left + minusButton.GetMarginBefore().X);
                minusButton.Events.OnClick = (Entity entity) =>
                {
                    var value = NumericValue - ButtonsStepSize;
                    if (value < MinValue) { value = MinValue.Value; }
                    if (value > MaxValue) { value = MaxValue.Value; }
                    NumericValue = value;
                };
                AddChildInternal(minusButton);
                _minusButton = minusButton;
            }
        }

        /// <summary>
        /// Create the numeric text input with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="addPlusButton">If true, will add a plus button to increase value.</param>
        /// <param name="addMinusButton">If true, will add a minus button to decrease value.</param>
        public NumericInput(UISystem system, bool addPlusButton = true, bool addMinusButton = true)
            : this(system, system.DefaultStylesheets.NumericTextInput ?? system.DefaultStylesheets.TextInput, addPlusButton, addMinusButton)
        {
        }

        /// <inheritdoc/>
        protected override int GetInputMaxWidth()
        {
            return base.GetInputMaxWidth() - (_plusButton != null ? _plusButton.LastBoundingRect.Width : 0);
        }

        /// <inheritdoc/>
        protected override bool IsValidValue(string value)
        {
            return TryParseValue(value, out _, out _);
        }


        /// <summary>
        /// Try to parse the given value according to our numeric logic.
        /// Succeeds if the given value is recognized and returns the actual decimal value and the base value to store. 
        /// </summary>
        /// <param name="value">The given input value, possibly misformatted</param>
        /// <param name="newValue">The succcessfully-parsed value to store</param>
        /// <param name="baseValue">Underlying base value that should be set on success, which may differ from input</param>
        bool TryParseValue(string value, out decimal? newValue, out string baseValue)
        {
            // remove decimal if not accepted
            if (!AcceptsDecimal)
            {
                value = value.Split(DecimalSeparator)[0];
            }

            // trim
            value = value.Trim();

            // if empty, stop here and set to empty
            if (value.Length == 0)
            {
                baseValue = string.Empty;
                newValue = null;
                return true;
            }
            
            // special case: fraction without leading 0
            // (attempting to parse this would fail, so we need to check it manually)
            if (value.Length == 1 && value[0] == DecimalSeparator)
            {
                baseValue = "0" + DecimalSeparator + value[(value.IndexOf(DecimalSeparator) + 1)..]; // "." -> "0."
                newValue = 0;
                return true;
            }
            
            // special case: negative fraction without leading 0
            // (attempting to parse this would fail, so we need to check it manually)
            if (value.Length == 2 && value[0] == NegativeSign && value[1] == DecimalSeparator)
            {
                baseValue = NegativeSign + "0" + DecimalSeparator + value[(value.IndexOf(DecimalSeparator) + 1)..]; // "-." -> "-0."
                newValue = 0;
                return true;
            }
            
            // special case: if the only input is - it might be the begining of a negative number, so we allow it
            if ((MinValue == null || MinValue.Value < 0) && (value.Length == 1 && value[0] == NegativeSign))
            {
                baseValue = value;
                newValue = null;
                return true;
            }
            
            // special case: negative sign is not leading
            // (attempting to parse this would succeed, but this isn't desired input)
            if (value.Length > 1 && value[0] != NegativeSign && value.Contains(NegativeSign))
            {
                // could not parse the value
                newValue = default;
                baseValue = default!;
                return false;
            }

            // set float value and base value
            if (decimal.TryParse(value, CultureInfo, out decimal result))
            {
                // check min value
                if (result < MinValue)
                {
                    result = MinValue.Value;
                    value = result.ToString(CultureInfo);
                }

                // check max value
                if (result > MaxValue)
                {
                    result = MaxValue.Value;
                    value = result.ToString(CultureInfo);
                }

                // check for redundant or missing zeros 
                if (value.Contains(DecimalSeparator))
                {
                    if (value[0] == NegativeSign)
                    {
                        if (value.Length > 1 && value[1] == DecimalSeparator)
                        {
                            value = value.Insert(1, "0"); // "-." -> "-0."
                        }
                        else if (value.Length > 3 && value[1] == '0' && value[2] != DecimalSeparator)
                        {
                            value = NegativeSign + value[1..].TrimStart('0'); // "-02.0" -> "-2.0"
                            
                            // did we overtrim? i.e. was the whole part was all 0s?
                            if (value[1] == DecimalSeparator)
                                value = NegativeSign + "0" + value[1..]; // "-.0" -> "-0.0"
                        }
                    }
                    else
                    {
                        if (value.Length > 2 && value[0] == '0' && value[1] != DecimalSeparator)
                        {
                            value = value.TrimStart('0'); // "01.2" -> "1.2"
                            
                            // did we overtrim? i.e. was the whole part was all 0s?
                            if (value[0] == DecimalSeparator)
                                value = "0" + value; // ".2" -> "0.2"
                        }
                        else if (value[0] == DecimalSeparator)
                        {
                            value = "0" + value; // "." -> "0."
                        }
                    }
                }
                else
                {
                    // if not 0, trim zeroes from the start
                    if (value.StartsWith('0') && value.Length > 1)
                    {
                        if (result == 0)
                            value = "0"; // "00" => "0"
                        else
                            value = value.TrimStart('0'); // "05" -> "5"
                    }
                    else if (value.Length > 2 && value[0] == NegativeSign && value[1] == '0')
                    {
                        if (result == 0)
                            value = NegativeSign + "0" + value[2..].TrimStart('0'); // "-00" -> "-0" 
                        else
                            value = NegativeSign + value[1..].TrimStart('0'); // "-05" -> "-5" 
                    }
                }

                // set value
                newValue = result;
                baseValue = value;
                return true;
            }

            // could not parse the value
            newValue = default;
            baseValue = default!;
            return false;
        }
    }
}
