using Iguina.Defs;
using System.Data;


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
        public string? PlaceholderText = null;

        /// <summary>
        /// Text input value.
        /// </summary>
        public string Value
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
        public bool Multiline
        {
            get => _multiline;
            set 
            {
                if (!value && _value.Contains('\n'))
                {
                    throw new Exception("Text input value contains line breaks, but multiline flag was turned to false!");
                }
                _multiline = value; 
                _valueParagraph.TextOverflowMode = value ? TextOverflowMode.WrapWords : TextOverflowMode.Overflow; 
            }
        }
        bool _multiline = false;

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <inheritdoc/>
        internal override bool LockFocusOnSelf => _lockSelf;
        bool _lockSelf = true;

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
        int _caretOffset;

        // caret actual line index in wrapped text
        int _caretActualLineIndexInWrappedText;

        /// <summary>
        /// Character to use as caret mark.
        /// </summary>
        public string CaretCharacter = "|";

        /// <summary>
        /// Caret blinking speed.
        /// </summary>
        public float CaretBlinkingSpeed = 3f;

        // for caret rendering
        int _caretOffsetInLine = 0;

        // for moving caret
        string _valueBeforeCaret = string.Empty;
        string _valueAfterCaret = string.Empty;

        // Convert offset in pixels to line first character index.
        Dictionary<int, int> _lineOffsetToIndex = new Dictionary<int, int>();

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

            // add blinking caret rendering + calculate actual caret position with word wrap applied
            _valueParagraph._beforeDrawingLineNoStyleCommands = (string line, int lineIndex, int lineStartIndex) =>
            {
                // reset if first line
                if (lineIndex == 0) 
                {
                    _lineOffsetToIndex.Clear();
                    _caretActualLineIndexInWrappedText = 0; 
                }

                // line not visible? skip
                int lineHeight = _valueParagraph.MeasureTextLineHeight();
                int offset = (lineIndex * lineHeight - (VerticalScrollbar?.Value ?? 0));
                _lineOffsetToIndex[offset] = lineStartIndex;

                // should we show caret?
                bool showCaret = (_isEditing && !string.IsNullOrEmpty(CaretCharacter));

                // are we in the correct line?
                if ((_caretOffset >= lineStartIndex) && (_caretOffset <= lineStartIndex + line.Length))
                {
                    // calculate which caret character to add
                    var charToAdd = (((int)(UISystem.ElapsedTime * CaretBlinkingSpeed)) % 2 == 0) ? CaretCharacter : " ";

                    // set caret actual line index
                    _caretActualLineIndexInWrappedText = lineIndex;

                    // draw caret
                    if (showCaret)
                    {
                        // calculate caret offset in current line
                        int caretOffsetInLine = _caretOffset - lineStartIndex;

                        // caret at the end of line
                        if (caretOffsetInLine >= line.Length)
                        {
                            return line + charToAdd;
                        }
                        // caret in middle of line
                        else
                        {
                            return line.Substring(0, caretOffsetInLine) + charToAdd + line.Substring(caretOffsetInLine);
                        }
                    }
                }
                return line;
            };

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
        protected override DrawMethodResult Draw(DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
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

            // call base drawing method
            var ret = base.Draw(parentDrawResult, siblingDrawResult);
            return ret;
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
            if (!Multiline && ((_valueParagraph.LastBoundingRect.Right + _valueParagraph.MeasureText(" ").X * 2) >= (LastInternalBoundingRect.Right - GetPadding().Right)))
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
            _valueBeforeCaret = Value.Substring(0, CaretOffset);
            _valueAfterCaret = Value.Substring(CaretOffset);
            _caretOffsetInLine = _caretOffset - Math.Max(0, Value.Substring(0, _caretOffset).LastIndexOf('\n') + 1);
        }

        /// <inheritdoc/>
        protected override int CalculateMaxScrollbarValue()
        {
            return Math.Max(1, (_valueParagraph.LastBoundingRect.Height + _valueParagraph?.StyleSheet?.Default?.FontSize ?? 20) - LastInternalBoundingRect.Height);
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
                var lineHeight = _valueParagraph.MeasureTextLineHeight();
                var caretOffsetY = (_caretActualLineIndexInWrappedText * lineHeight);
                var absCaretOffsetY = caretOffsetY + _valueParagraph.Offset.Y.Value;
                if ((absCaretOffsetY > (LastBoundingRect.Height - lineHeight * 3)) || (absCaretOffsetY < (lineHeight * 2)))
                {
                    VerticalScrollbar.ValueSafe = (int)(caretOffsetY) - lineHeight * 2;
                }
            }

            // select line to edit
            if (inputState.LeftMouseDown)
            {
                int lineHeight = _valueParagraph.MeasureTextLineHeight();
                foreach (var lineData in _lineOffsetToIndex)
                {
                    int cpY = inputState.MousePosition.Y - LastBoundingRect.Y - lineHeight / 2;
                    if (cpY >= lineData.Key && cpY <= lineData.Key + lineHeight)
                    {
                        CaretOffset = lineData.Value;
                        MoveCaretToEndOfLine();
                    }
                }
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
                if (inputState.LeftMouseDown && !IsPointedOn(inputState.MousePosition, true))
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
                                if (_valueBeforeCaret.Contains('\n'))
                                {
                                    CaretOffset = Math.Max(0, GetSecondLineBreakFromEnd(_valueBeforeCaret)) + _caretOffsetInLine + 1;
                                    var lastLineBreak = _valueBeforeCaret.LastIndexOf('\n');
                                    if (CaretOffset > lastLineBreak)
                                    {
                                        CaretOffset = lastLineBreak;
                                    }
                                }
                                else
                                {
                                    CaretOffset = 0;
                                }
                            }
                            // move caret down
                            else if (cmd == Drivers.TextInputCommands.MoveCaretDown)
                            {
                                if (_valueAfterCaret.Contains('\n'))
                                {
                                    var nextLineBreakOffset = _valueAfterCaret.IndexOf('\n', _valueAfterCaret[0] == '\n' ? 1 : 0) + 1;
                                    if (nextLineBreakOffset > 0)
                                    {
                                        CaretOffset += nextLineBreakOffset + _caretOffsetInLine;
                                    }
                                    else
                                    {
                                        CaretOffset += _caretOffsetInLine;
                                    }
                                }
                                else
                                {
                                    CaretOffset = Value.Length;
                                }
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

        /// <summary>
        /// Get second line break from end.
        /// </summary>
        int GetSecondLineBreakFromEnd(string value)
        {
            int count = 0;
            int secondLastIndex = -1;

            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (value[i] == '\n')
                {
                    count++;
                    if (count == 2)
                    {
                        secondLastIndex = i;
                        break;
                    }
                }
            }

            return secondLastIndex;
        }
    }
}
