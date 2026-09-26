using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// Text input entity.
    /// </summary>
    public class TextInput : Panel
    {
        /// <summary>
        /// Text to show if input is empty.
        /// </summary>
        public virtual string? PlaceholderText
        {
            get => _placeholder;
            set => _placeholder = value;
        }
        string? _placeholder;

        /// <summary>
        /// Text input value.
        /// </summary>
        public virtual string Value
        {
            get => _value;
            set 
            {
                if (value.Contains('\n') && !Multiline)
                {
                    throw new Exception("Text input value contains line breaks, but multiline flag is set to false!");
                }

                if (value != _value)
                {
                    _value = value;
                    
                    // Make sure the caret doesn't overflow if, for example, the value is being directly manipulated while the user is typing (e.g. NumericInput)
                    if (_caretOffset > Value.Length)
                        _caretOffset = Value.Length;
                    
                    Events.OnValueChanged?.Invoke(this);
                    UISystem.Events.OnValueChanged?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// If set, will show this character instead of the actual input value.
        /// Used for password input fields.
        /// </summary>
        public char? MaskingCharacter = null;

        // current value
        string _value = string.Empty;

        // to show text value 
        Paragraph _valueParagraph;

        // if true, it means the user clicked on this element is its currently being edited
        bool _isEditing = false;

        /// <summary>
        /// If true, text input will support line breaks.
        /// </summary>
        public virtual bool Multiline
        {
            get => _multiline;
            set 
            {
                if (!value && _value.Contains('\n'))
                {
                    throw new InvalidOperationException("Text input value contains line breaks, but multiline flag was turned to false!");
                }
                _multiline = value; 
                _valueParagraph.TextOverflowMode = value ? TextOverflowMode.WrapWords : TextOverflowMode.Overflow; 
            }
        }
        bool _multiline = false;

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <inheritdoc/>
        internal override bool LockTargetedEntityOnSelf => _lockSelf;
        internal bool _lockSelf = true;

        /// <summary>
        /// Max lines limit, when multiline input is true.
        /// </summary>
        public int? MaxLines;

        /// <summary>
        /// Limit text input max length, in characters count.
        /// </summary>
        public int? MaxLength;

        /// <summary>
        /// Text editor caret offset.
        /// </summary>
        public int CaretOffset
        {
            get => _caretOffset;
            set => _caretOffset = Math.Clamp(value, 0, Value.Length);
        }
        protected int _caretOffset;

        /// <summary>
        /// Character to use as caret mark.
        /// </summary>
        public string CaretCharacter = "|";

        /// <summary>
        /// Caret blinking speed.
        /// </summary>
        public float CaretBlinkingSpeed = 3f;

        // desired caret X position when moving caret up and down, so it will keep its column when passing through shorter lines
        int? _caretDesiredX;

        /// <summary>
        /// Create the text input.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Text input stylesheet.</param>
        public TextInput(UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
        {
            // create paragraph to display the input text
            _valueParagraph = new Paragraph(system, stylesheet);
            _valueParagraph.CopyStateFrom = this;
            _valueParagraph.DrawFillTexture = false;
            _valueParagraph.IgnoreInteractions = true;
            _valueParagraph.TransferInteractionsTo = this;
            _valueParagraph.EnableStyleCommands = false;

            // hide overflow by default
            OverflowMode = OverflowMode.HideOverflow;

            AddChildInternal(_valueParagraph);
        }

        /// <summary>
        /// Create the text input with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public TextInput(UISystem system) : this(system, system.DefaultStylesheets.TextInput)
        {
        }

        /// <inheritdoc/>
        protected override DrawMethodResult Draw(DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult, bool dryRun)
        {
            // set text to show
            UpdateParagraphText();

            // set blinking caret
            bool showCaret = _isEditing && (((int)(UISystem.ElapsedTime * CaretBlinkingSpeed)) % 2 == 0);
            _valueParagraph._caretText = CaretCharacter;
            _valueParagraph._caretSourceIndex = showCaret ? CaretOffset : -1;

            // call base drawing method
            var ret = base.Draw(parentDrawResult, siblingDrawResult, dryRun);
            return ret;
        }

        /// <summary>
        /// Update the paragraph text to show value, mask or placeholder.
        /// </summary>
        void UpdateParagraphText()
        {
            // do we currently have a value?
            var noValue = string.IsNullOrEmpty(Value);

            // set text to placeholder or empty
            if (noValue)
            {
                _valueParagraph.Text = _isEditing ? "\r" : (PlaceholderText ?? string.Empty);
            }
            // set text to value or mask
            else
            {
                if (MaskingCharacter == null)
                {
                    _valueParagraph.Text = Value;
                }
                else
                {
                    _valueParagraph.Text = new string(MaskingCharacter.Value, Value.Length);
                }
            }
            _valueParagraph.UseEmptyValueTextColor = noValue;
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPercents(100f);
            ret.Y.SetPixels(40);
            return ret;
        }

        /// <summary>
        /// Insert characters at caret position, from string value.
        /// </summary>
        /// <param name="characters">Character(s) string value.</param>
        /// <returns>How many characters were actually added.</returns>
        public int InsertCharacters(string characters)
        {
            // multiple characters insertion
            if (characters.Length > 1)
            {
                int added = 0;
                for (int i = 0; i < characters.Length; ++i)
                {
                    added += InsertCharacters(characters.Substring(i, 1));
                }
                return added;
            }

            // check multiline / rc
            if (characters == "\n")
            {
                if (!Multiline) 
                { 
                    return 0; 
                }
                if (MaxLines.HasValue && (Value.Count(x => x == '\n') >= MaxLines.Value)) 
                { 
                    return 0; 
                }
            }
            if (characters == "\r") { return 0; }

            // check max length
            if (MaxLength.HasValue && Value.Length > MaxLength.Value) { return 0; }

            // check max width
            if (!Multiline && ((_valueParagraph.LastBoundingRect.Right + _valueParagraph.MeasureText(" ").X * 2) >= GetInputMaxWidth()))
            {
                return 0;
            }

            // add value
            if (CaretOffset == Value.Length)
            {
                Value += characters;
            }
            else
            {
                Value = Value.Insert(CaretOffset, characters);
            }

            // update caret
            CaretOffset++;
            return 1;
        }

        /// <summary>
        /// Get max width for text input.
        /// </summary>
        protected virtual int GetInputMaxWidth()
        {
            return (LastInternalBoundingRect.Right - GetPadding().Right);
        }

        /// <summary>
        /// Insert character at caret position, from unicode value.
        /// </summary>
        /// <param name="unicode">Character unicode value.</param>
        public void InsertCharacter(int unicode)
        {
            var character = Char.ConvertFromUtf32(unicode);
            InsertCharacters(character);
        }

        /// <summary>
        /// Insert character at caret position, from ascii value.
        /// </summary>
        /// <param name="ascii">Character ascii value.</param>
        public void InsertCharacter(char ascii)
        {
            var character = Char.ConvertFromUtf32(ascii);
            InsertCharacters(character);
        }

        /// <inheritdoc/>
        internal override void PostUpdate(InputState inputState)
        {
            base.PostUpdate(inputState);

            // skip if not visible
            if (!IsCurrentlyVisible()) { return; }

            // set if editing
            if (_isEditing && !IsTargeted) 
            { 
                _isEditing = false; 
            }

            // if clicked outside, release lock
            if (_isEditing && !_isDraggingScrollbar && inputState.LeftMouseDown && !IsPointedOn(inputState.MousePosition, true))
            {
                _isEditing = false;
                _lockSelf = false;
            }

            // if editing, calculate some useful values
            if (_isEditing)
            {
                UpdateCachedCaretValues();
            }
        }

        /// <summary>
        /// Update caret related cached values.
        /// </summary>
        void UpdateCachedCaretValues()
        {
            CaretOffset = CaretOffset; // to make sure caret is within valid range
        }

        /// <summary>
        /// Move caret to previous / next visual line (taking word wrap into account), while trying to keep its X position.
        /// </summary>
        /// <param name="direction">-1 to move up, 1 to move down.</param>
        void MoveCaretVertically(int direction)
        {
            UpdateParagraphText();
            int linesCount = _valueParagraph.EnsureProcessedText();
            var caretPosition = _valueParagraph.GetSourceIndexPosition(CaretOffset, out int caretLine);
            _caretDesiredX ??= caretPosition.X;

            int targetLine = caretLine + direction;
            if (targetLine < 0)
            {
                CaretOffset = 0;
            }
            else if (targetLine >= linesCount)
            {
                CaretOffset = Value.Length;
            }
            else
            {
                CaretOffset = _valueParagraph.GetSourceIndexAtLineAndX(targetLine, _caretDesiredX.Value);
            }
        }

        /// <inheritdoc/>
        protected override int CalculateMaxScrollbarValue()
        {
            return Math.Max(1, (_valueParagraph.LastBoundingRect.Height + (_valueParagraph.StyleSheet?.Default?.FontSize ?? 20)) - LastInternalBoundingRect.Height);
        }

        /// <summary>
        /// Move caret to end of current line.
        /// </summary>
        void MoveCaretToEndOfLine()
        {
            _caretOffset = Math.Clamp(_caretOffset, 0, Value.Length);
            while ((_caretOffset < Value.Length) && (Value[_caretOffset] != '\n'))
            {
                _caretOffset++;
            }
        }

        /// <summary>
        /// Move caret to start of current line.
        /// </summary>
        void MoveCaretToStartOfLine()
        {
            _caretOffset = Math.Clamp(_caretOffset, 0, Value.Length);
            while ((_caretOffset > 0) && (Value[_caretOffset - 1] != '\n'))
            {
                _caretOffset--;
            }
        }

        /// <inheritdoc/>
        internal override void DoInteractions(InputState inputState)
        {
            base.DoInteractions(inputState);

            // check if need to adjust scrollbar to make sure caret is visible
            if (_needToMakeSureCaretIsVisible && (VerticalScrollbar != null))
            {
                _needToMakeSureCaretIsVisible = false;
                UpdateCachedCaretValues();
                UpdateParagraphText();
                var lineHeight = _valueParagraph.MeasureTextLineHeight();
                var caretOffsetY = (_valueParagraph.GetLineIndexOfSourceIndex(CaretOffset) * lineHeight);
                var absCaretOffsetY = caretOffsetY + _valueParagraph.Offset.Y.Value;
                if ((absCaretOffsetY > (LastBoundingRect.Height - lineHeight * 3)) || (absCaretOffsetY < (lineHeight * 2)))
                {
                    VerticalScrollbar.ValueSafe = (int)(caretOffsetY) - lineHeight * 2;
                }
            }

            // while editing, this entity locks targeting on itself, so the scrollbar won't get interactions on its own.
            // if mouse was pressed on the scrollbar, pass interactions to it until mouse is released.
            if (inputState.LeftMousePressedNow)
            {
                _isDraggingScrollbar = (VerticalScrollbar != null) && VerticalScrollbar.IsCurrentlyVisible() && VerticalScrollbar.IsPointedOn(inputState.MousePosition);
            }
            else if (!inputState.LeftMouseDown)
            {
                _isDraggingScrollbar = false;
            }
            if (_isDraggingScrollbar)
            {
                if (!VerticalScrollbar!.IsCurrentlyDisabled() && !VerticalScrollbar.IsCurrentlyLocked())
                {
                    VerticalScrollbar.DoInteractions(inputState);
                }
            }
            // set caret position from click
            else if (inputState.LeftMouseDown)
            {
                if (!string.IsNullOrEmpty(Value))
                {
                    UpdateParagraphText();
                    CaretOffset = _valueParagraph.GetSourceIndexAtPosition(inputState.MousePosition);
                }
                _caretDesiredX = null;
                _needToMakeSureCaretIsVisible = true;
            }

            // lock editing
            if (inputState.LeftMouseDown || inputState.RightMouseDown)
            {
                _isEditing = true;
            }

            // if clicked on editing, decide if should lock target entity or not
            if (_isEditing)
            {
                if (!_isDraggingScrollbar && inputState.LeftMouseDown && !IsPointedOn(inputState.MousePosition, true))
                {
                    _lockSelf = false;
                }
                else
                {
                    _lockSelf = true;
                }
            }
            else
            {
                _lockSelf = false;
            }

            // get input if editing
            if (_isEditing)
            {
                // did we type anything?
                bool didType = false;

                // get text input
                if (inputState.TextInput != null)
                {
                    foreach (var unicode in inputState.TextInput)
                    {
                        InsertCharacter(unicode);
                        _caretDesiredX = null;
                        didType = true;
                    }
                }

                // apply special input commands
                if (inputState.TextInputCommands != null)
                {
                    if (Value.Length > 0)
                    {
                        foreach (var cmd in inputState.TextInputCommands)
                        {
                            // any command other than moving up / down resets the desired caret X position
                            if ((cmd != Drivers.TextInputCommands.MoveCaretUp) && (cmd != Drivers.TextInputCommands.MoveCaretDown))
                            {
                                _caretDesiredX = null;
                            }

                            // backspace
                            if (cmd == Drivers.TextInputCommands.Backspace)
                            {
                                try
                                {
                                    Value = Value.Remove(CaretOffset - 1, 1);
                                    CaretOffset--;
                                }
                                catch { }
                            }
                            // delete
                            else if (cmd == Drivers.TextInputCommands.Delete)
                            {
                                try
                                {
                                    if (CaretOffset < Value.Length)
                                    {
                                        Value = Value.Remove(CaretOffset, 1);
                                    }
                                }
                                catch { }
                            }
                            // move caret left
                            else if (cmd == Drivers.TextInputCommands.MoveCaretLeft)
                            {
                                CaretOffset--;
                            }
                            // move caret right
                            else if (cmd == Drivers.TextInputCommands.MoveCaretRight)
                            {
                                CaretOffset++;
                            }
                            // move caret up
                            else if (cmd == Drivers.TextInputCommands.MoveCaretUp)
                            {
                                MoveCaretVertically(-1);
                            }
                            // move caret down
                            else if (cmd == Drivers.TextInputCommands.MoveCaretDown)
                            {
                                MoveCaretVertically(1);
                            }
                            // move caret to end
                            else if (cmd == Drivers.TextInputCommands.MoveCaretEnd)
                            {
                                CaretOffset = Value.Length;
                            }
                            // move caret to home
                            else if (cmd == Drivers.TextInputCommands.MoveCaretStart)
                            {
                                CaretOffset = 0;
                            }
                            // move caret to start of line
                            else if (cmd == Drivers.TextInputCommands.MoveCaretEndOfLine)
                            {
                                MoveCaretToEndOfLine();
                            }
                            // move caret to end of line
                            else if (cmd == Drivers.TextInputCommands.MoveCaretStartOfLine)
                            {
                                MoveCaretToStartOfLine();
                            }
                            // break line
                            else if (cmd == Drivers.TextInputCommands.BreakLine)
                            {
                                InsertCharacter('\n');
                            }

                            didType = true;
                        }
                    }
                }

                // if typed, make sure caret is visible
                if (didType && (VerticalScrollbar != null))
                {
                    _needToMakeSureCaretIsVisible = true;
                }
            }
        }

        // if true, it means we need to check if caret is currently visible after next update call
        bool _needToMakeSureCaretIsVisible = false;

        // if true, mouse was pressed on the scrollbar and is still down
        bool _isDraggingScrollbar = false;
    }
}
