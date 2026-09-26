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
        /// If true, will allow selecting text by dragging the mouse, or by using arrow keys while shift is down.
        /// </summary>
        /// <remarks>Selected text is highlighted with the 'TextHighlightColor' style property.</remarks>
        public bool AllowTextSelection = true;

        /// <summary>
        /// Selection offset, relative to caret offset.
        /// Positive value means selection goes after the caret, negative value means selection goes before the caret.
        /// For example, caret offset of 10 and selection offset of -5 means text range [5, 10) is selected.
        /// </summary>
        /// <remarks>Value is always capped to text length. Will always be 0 if text selection is not allowed.</remarks>
        public int SelectionOffset
        {
            get => AllowTextSelection ? (Math.Clamp(CaretOffset + _selectionOffset, 0, Value.Length) - CaretOffset) : 0;
            set => _selectionOffset = value;
        }
        int _selectionOffset;

        /// <summary>
        /// Selected text range start index (inclusive).
        /// </summary>
        public int SelectionStart => Math.Min(CaretOffset, CaretOffset + SelectionOffset);

        /// <summary>
        /// Selected text range end index (exclusive).
        /// </summary>
        public int SelectionEnd => Math.Max(CaretOffset, CaretOffset + SelectionOffset);

        /// <summary>
        /// Selected text length, in characters.
        /// </summary>
        public int SelectionLength => SelectionEnd - SelectionStart;

        /// <summary>
        /// Returns true if there's currently selected text.
        /// </summary>
        public bool HasSelection => SelectionLength > 0;

        /// <summary>
        /// Get currently selected text, or empty string if there's no selection.
        /// </summary>
        public string SelectedText => HasSelection ? Value.Substring(SelectionStart, SelectionLength) : string.Empty;

        /// <summary>
        /// Set selected text range.
        /// Caret will be placed at 'end', and selection will extend from it towards 'start'.
        /// Values are capped to text length.
        /// </summary>
        /// <param name="start">Selection start index (where the selection begins from).</param>
        /// <param name="end">Selection end index (where the caret will be placed).</param>
        /// <remarks>If text selection is not allowed, will just set caret offset to 'end'.</remarks>
        public void SetSelection(int start, int end)
        {
            start = Math.Clamp(start, 0, Value.Length);
            CaretOffset = end;
            _selectionOffset = AllowTextSelection ? (start - CaretOffset) : 0;
        }

        /// <summary>
        /// Get selected text range.
        /// </summary>
        /// <param name="start">Selection start index (inclusive).</param>
        /// <param name="end">Selection end index (exclusive).</param>
        /// <returns>True if there's currently selected text.</returns>
        public bool GetSelection(out int start, out int end)
        {
            start = SelectionStart;
            end = SelectionEnd;
            return end > start;
        }

        /// <summary>
        /// Select the entire text, with caret at the end.
        /// </summary>
        public void SelectAll()
        {
            SetSelection(0, Value.Length);
        }

        /// <summary>
        /// Clear text selection, without changing caret position.
        /// </summary>
        public void ClearSelection()
        {
            _selectionOffset = 0;
        }

        /// <summary>
        /// Delete currently selected text, and move caret to where the selection started.
        /// </summary>
        /// <returns>True if selected text was deleted.</returns>
        public bool DeleteSelection()
        {
            if (!HasSelection) { return false; }
            var start = SelectionStart;
            var prevValue = Value;
            Value = Value.Remove(start, SelectionLength);
            if (Value == prevValue) { return false; } // value was rejected (for example, by numeric input validation)
            CaretOffset = start;
            _selectionOffset = 0;
            return true;
        }

        /// <summary>
        /// Replace currently selected text (or insert at caret position, if there's no selection) with a given text.
        /// </summary>
        /// <param name="text">Text to put instead of the selected text.</param>
        /// <returns>How many characters were actually added.</returns>
        public int ReplaceSelection(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                DeleteSelection();
                return 0;
            }
            return InsertCharacters(text);
        }

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

            // set selection to highlight
            bool showSelection = _isEditing && HasSelection;
            _valueParagraph._selectionStart = showSelection ? SelectionStart : 0;
            _valueParagraph._selectionEnd = showSelection ? SelectionEnd : 0;
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
        /// <remarks>If there's selected text, it will be replaced with the new characters.</remarks>
        public int InsertCharacters(string characters)
        {
            // replace selected text
            var prevValue = Value;
            var prevCaret = CaretOffset;
            var prevSelection = _selectionOffset;
            bool deletedSelection = (characters.Length > 0) && DeleteSelection();

            // insert characters one by one
            int added = 0;
            for (int i = 0; i < characters.Length; ++i)
            {
                added += InsertSingleCharacter(characters.Substring(i, 1));
            }

            // if nothing was added, restore the deleted selection (for example, line break in a single line input, or invalid character in numeric input)
            if (deletedSelection && (added == 0))
            {
                Value = prevValue;
                CaretOffset = prevCaret;
                _selectionOffset = prevSelection;
            }
            return added;
        }

        /// <summary>
        /// Insert a single character at caret position.
        /// </summary>
        /// <returns>1 if character was added, 0 otherwise.</returns>
        int InsertSingleCharacter(string characters)
        {
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
            if (MaxLength.HasValue && Value.Length >= MaxLength.Value) { return 0; }

            // check max width
            // note: measure current text and not paragraph last bounding rect, since value may have changed since last draw (for example, if we just deleted selected text)
            if (!Multiline)
            {
                var displayedText = (MaskingCharacter == null) ? Value : new string(MaskingCharacter.Value, Value.Length);
                if ((_valueParagraph.LastBoundingRect.Left + _valueParagraph.MeasureText(displayedText).X + _valueParagraph.MeasureText(" ").X * 2) >= GetInputMaxWidth())
                {
                    return 0;
                }
            }

            // add value
            var prevValue = Value;
            Value = Value.Insert(CaretOffset, characters);

            // value was rejected (for example, by numeric input validation)
            if (Value == prevValue)
            {
                return 0;
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

            // while editing we lock targeting on self and pass interactions to the scrollbar, so we need to set its state to show it as pointed on / pressed
            if (VerticalScrollbar != null)
            {
                EntityState? scrollbarState = null;
                if (_isEditing && IsTargeted && !VerticalScrollbar.IsCurrentlyDisabled())
                {
                    if (_isDraggingScrollbar)
                    {
                        scrollbarState = EntityState.Interacted;
                    }
                    else if (VerticalScrollbar.IsPointedOn(inputState.MousePosition))
                    {
                        scrollbarState = EntityState.Targeted;
                    }
                }
                VerticalScrollbar.LockedState = scrollbarState;
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
        /// Get text position one visual line above / below a given position (taking word wrap into account), while trying to keep its X position.
        /// </summary>
        /// <param name="position">Position to move from.</param>
        /// <param name="direction">-1 to move up, 1 to move down.</param>
        int GetVerticalMovePosition(int position, int direction)
        {
            UpdateParagraphText();
            int linesCount = _valueParagraph.EnsureProcessedText();
            var screenPosition = _valueParagraph.GetSourceIndexPosition(position, out int line);
            _caretDesiredX ??= screenPosition.X;

            int targetLine = line + direction;
            if (targetLine < 0)
            {
                return 0;
            }
            else if (targetLine >= linesCount)
            {
                return Value.Length;
            }
            return _valueParagraph.GetSourceIndexAtLineAndX(targetLine, _caretDesiredX.Value);
        }

        /// <inheritdoc/>
        protected override int CalculateMaxScrollbarValue()
        {
            return Math.Max(1, (_valueParagraph.LastBoundingRect.Height + (_valueParagraph.StyleSheet?.Default?.FontSize ?? 20)) - LastInternalBoundingRect.Height);
        }

        /// <summary>
        /// Get end of line position, for a given position.
        /// </summary>
        int GetEndOfLinePosition(int position)
        {
            position = Math.Clamp(position, 0, Value.Length);
            while ((position < Value.Length) && (Value[position] != '\n'))
            {
                position++;
            }
            return position;
        }

        /// <summary>
        /// Get start of line position, for a given position.
        /// </summary>
        int GetStartOfLinePosition(int position)
        {
            position = Math.Clamp(position, 0, Value.Length);
            while ((position > 0) && (Value[position - 1] != '\n'))
            {
                position--;
            }
            return position;
        }

        /// <summary>
        /// Get the position we get to by applying a caret movement command on a given position.
        /// </summary>
        /// <returns>New position, or null if command is not a movement command.</returns>
        int? GetPositionAfterMovement(int position, Drivers.TextInputCommands command)
        {
            switch (command)
            {
                case Drivers.TextInputCommands.MoveCaretLeft: return Math.Max(0, position - 1);
                case Drivers.TextInputCommands.MoveCaretRight: return Math.Min(Value.Length, position + 1);
                case Drivers.TextInputCommands.MoveCaretUp: return GetVerticalMovePosition(position, -1);
                case Drivers.TextInputCommands.MoveCaretDown: return GetVerticalMovePosition(position, 1);
                case Drivers.TextInputCommands.MoveCaretEnd: return Value.Length;
                case Drivers.TextInputCommands.MoveCaretStart: return 0;
                case Drivers.TextInputCommands.MoveCaretEndOfLine: return GetEndOfLinePosition(position);
                case Drivers.TextInputCommands.MoveCaretStartOfLine: return GetStartOfLinePosition(position);
                default: return null;
            }
        }

        /// <summary>
        /// Get text position under a given point on screen.
        /// </summary>
        int GetPositionAtPoint(Point point)
        {
            if (string.IsNullOrEmpty(Value)) { return 0; }
            UpdateParagraphText();
            return Math.Clamp(_valueParagraph.GetSourceIndexAtPosition(point), 0, Value.Length);
        }

        /// <inheritdoc/>
        internal override void DoInteractions(InputState inputState)
        {
            base.DoInteractions(inputState);

            // check if need to adjust scrollbar to make sure caret (or selection end, if we're selecting) is visible
            if (_needToMakeSureCaretIsVisible && (VerticalScrollbar != null))
            {
                _needToMakeSureCaretIsVisible = false;
                UpdateCachedCaretValues();
                UpdateParagraphText();
                var lineHeight = _valueParagraph.MeasureTextLineHeight();
                var lineY = _valueParagraph.GetLineIndexOfSourceIndex(CaretOffset + SelectionOffset) * lineHeight;
                var visibleHeight = LastInternalBoundingRect.Height;
                if (lineY < VerticalScrollbar.Value)
                {
                    VerticalScrollbar.ValueSafe = lineY;
                }
                else if (lineY + lineHeight > VerticalScrollbar.Value + visibleHeight)
                {
                    VerticalScrollbar.ValueSafe = lineY + lineHeight - visibleHeight;
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
                _isDraggingSelection = false;
            }
            if (_isDraggingScrollbar)
            {
                if (!VerticalScrollbar!.IsCurrentlyDisabled() && !VerticalScrollbar.IsCurrentlyLocked())
                {
                    VerticalScrollbar.DoInteractions(inputState);
                }
            }
            // clicking sets caret position and resets selection, dragging while mouse is down selects text
            else if (inputState.LeftMouseDown)
            {
                var position = GetPositionAtPoint(inputState.MousePosition);
                if (inputState.LeftMousePressedNow)
                {
                    // shift + click extends selection from caret
                    if (AllowTextSelection && _isEditing && inputState.ShiftDown)
                    {
                        _selectionOffset = position - CaretOffset;
                    }
                    else
                    {
                        CaretOffset = position;
                        _selectionOffset = 0;
                    }
                    _isDraggingSelection = IsPointedOn(inputState.MousePosition, true);
                }
                else if (_isDraggingSelection)
                {
                    if (AllowTextSelection)
                    {
                        _selectionOffset = position - CaretOffset;
                    }
                    else
                    {
                        CaretOffset = position;
                    }
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
                if (!_isDraggingScrollbar && !_isDraggingSelection && inputState.LeftMouseDown && !IsPointedOn(inputState.MousePosition, true))
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
                            if (!DeleteSelection() && (CaretOffset > 0))
                            {
                                var caret = CaretOffset;
                                Value = Value.Remove(caret - 1, 1);
                                CaretOffset = caret - 1;
                            }
                        }
                        // delete
                        else if (cmd == Drivers.TextInputCommands.Delete)
                        {
                            if (!DeleteSelection() && (CaretOffset < Value.Length))
                            {
                                Value = Value.Remove(CaretOffset, 1);
                            }
                        }
                        // break line
                        else if (cmd == Drivers.TextInputCommands.BreakLine)
                        {
                            InsertCharacter('\n');
                        }
                        // caret movement
                        else
                        {
                            // shift is down: move the selection end, while caret stays in place
                            if (AllowTextSelection && inputState.ShiftDown)
                            {
                                var newSelectionEnd = GetPositionAfterMovement(CaretOffset + SelectionOffset, cmd);
                                if (newSelectionEnd.HasValue)
                                {
                                    _selectionOffset = newSelectionEnd.Value - CaretOffset;
                                }
                            }
                            // moving left / right with selection: go to selection start / end
                            else if (HasSelection && ((cmd == Drivers.TextInputCommands.MoveCaretLeft) || (cmd == Drivers.TextInputCommands.MoveCaretRight)))
                            {
                                CaretOffset = (cmd == Drivers.TextInputCommands.MoveCaretLeft) ? SelectionStart : SelectionEnd;
                                _selectionOffset = 0;
                            }
                            // regular caret movement
                            else
                            {
                                var newCaret = GetPositionAfterMovement(CaretOffset, cmd);
                                if (newCaret.HasValue)
                                {
                                    CaretOffset = newCaret.Value;
                                    _selectionOffset = 0;
                                }
                            }
                        }

                        didType = true;
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

        // if true, mouse was pressed on the text and is still down (so dragging it will select text)
        bool _isDraggingSelection = false;
    }
}
