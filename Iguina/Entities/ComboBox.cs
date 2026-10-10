using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A drop down list with an editable text box.
    /// Users can pick a value from the list, or type to filter the list and select an item by its text.
    /// If 'AllowFreeText' is true, users can also type any value that is not in the list.
    /// </summary>
    public class ComboBox : DropDown
    {
        /// <summary>
        /// If true, users can type any free text as value, even if it doesn't match any of the list items.
        /// If false, typed text is only used to search the list, and the value must be one of the list items. In this mode, when the text is committed (on enter, on closing the list, or when the text box loses focus), text that doesn't match any item is reverted to the selected item text, and empty text clears the selection.
        /// </summary>
        public bool AllowFreeText = true;

        /// <summary>
        /// If true, while typing, the opened list will only show items that contain the typed text.
        /// </summary>
        public bool FilterItemsByText = true;

        /// <summary>
        /// If true, typing in the text box will open the list.
        /// </summary>
        public bool OpenListWhileTyping = true;

        /// <summary>
        /// If true, clicking on the text box will open the list.
        /// </summary>
        public bool OpenListOnClick = true;

        /// <summary>
        /// If true, matching typed text to list items (and filtering the list) is case sensitive.
        /// </summary>
        public bool CaseSensitive = false;

        /// <summary>
        /// The text input entity used to type the combo box text.
        /// Use this to set things like max length, or override text styles.
        /// </summary>
        public TextInput TextInputField => _textInput;
        ComboBoxTextInput _textInput;

        /// <summary>
        /// Text to show in the text box while its empty.
        /// </summary>
        public string? PlaceholderText
        {
            get => _textInput.PlaceholderText;
            set => _textInput.PlaceholderText = value;
        }

        /// <summary>
        /// Get / set the text currently in the combo box text box.
        /// If the text matches a list item text, this item will be selected.
        /// If 'AllowFreeText' is false and the text doesn't match any item, the text will revert to the selected item text (or clear the selection, if text is empty).
        /// </summary>
        public string Text
        {
            get => _textInput.Value;
            set
            {
                _settingTextByCode = true;
                try
                {
                    _textInput.Value = value;
                }
                finally
                {
                    _settingTextByCode = false;
                }
                if (!AllowFreeText)
                {
                    CommitText();
                }
            }
        }

        /// <summary>
        /// Get / set the combo box value.
        /// If an item is selected, this is the selected item value. Otherwise, if 'AllowFreeText' is true and text is not empty, this is the free text.
        /// Null if there's no value.
        /// When setting a value that is not one of the items values, it will be set as free text, or throw exception if 'AllowFreeText' is false.
        /// </summary>
        public string? Value
        {
            get
            {
                if (SelectedValue != null) { return SelectedValue; }
                if (AllowFreeText && !string.IsNullOrEmpty(Text)) { return Text; }
                return null;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    SelectedValue = null;
                    Text = string.Empty;
                }
                else if (GetIndexOfValue(value) >= 0)
                {
                    SelectedValue = value;
                    SyncTextToSelection();
                }
                else if (AllowFreeText)
                {
                    Text = value;
                }
                else
                {
                    throw new KeyNotFoundException("Value to set was not found in list, and combo box does not allow free text!");
                }
            }
        }

        /// <summary>
        /// Returns true if the combo box has a value that is free text, and not one of the list items.
        /// </summary>
        public bool IsFreeText => (SelectedValue == null) && AllowFreeText && !string.IsNullOrEmpty(Text);

        // text typed by the user to filter list items by
        string _filterText = string.Empty;

        // to prevent recursion when syncing text with selection
        bool _syncingTextFromSelection = false;
        bool _syncingSelectionFromText = false;
        bool _settingTextByCode = false;

        // last value we reported in value changed events
        string? _lastReportedValue = null;

        // was text input in edit mode last update
        bool _wasEditing = false;

        /// <summary>
        /// Create the combo box.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Combo box panel stylesheet.</param>
        /// <param name="itemsStylesheet">Combo box items stylesheet. If not set, will use the same as base stylesheet.</param>
        /// <param name="arrowIconStylesheet">Stylesheet for an arrow icon to add to the combo box to reflect its state. If null, will not add this entity.</param>
        /// <param name="textInputStylesheet">Stylesheet for the text input to type values. If null, will use the items stylesheet.</param>
        public ComboBox(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null, StyleSheet? arrowIconStylesheet = null, StyleSheet? textInputStylesheet = null) : base(system, stylesheet, itemsStylesheet, arrowIconStylesheet)
        {
            // clicking an item that is already selected should not clear the value
            AllowDeselect = false;

            // hide the selected value paragraph, the text input will show the value instead
            SelectedValueParagraph.Visible = false;

            // create the text input on top of the selected value panel
            _textInput = new ComboBoxTextInput(this, system, textInputStylesheet ?? itemsStylesheet ?? stylesheet);
            _textInput.Anchor = Anchor.TopLeft;
            _textInput.IgnoreScrollOffset = true;
            _textInput.Size.X.SetPercents(100f);
            _textInput.Size.Y.SetPixels(ItemHeight);
            _textInput.Events.OnValueChanged += (Entity entity) => OnTextChanged();
            AddChildInternal(_textInput, true);
        }

        /// <summary>
        /// Create the combo box with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public ComboBox(UISystem system) : this(system,
            system.DefaultStylesheets.ComboBoxPanels ?? system.DefaultStylesheets.DropDownPanels ?? system.DefaultStylesheets.ListPanels ?? system.DefaultStylesheets.Panels,
            system.DefaultStylesheets.ComboBoxItems ?? system.DefaultStylesheets.DropDownItems ?? system.DefaultStylesheets.ListItems ?? system.DefaultStylesheets.Paragraphs,
            system.DefaultStylesheets.ComboBoxIcon ?? system.DefaultStylesheets.DropDownIcon,
            system.DefaultStylesheets.ComboBoxTextInput)
        {
        }

        /// <summary>
        /// Get the text to show and match for a given list item.
        /// </summary>
        string GetItemText(ListItem item)
        {
            return item.LabelTextOnly ?? item.Value;
        }

        /// <summary>
        /// Get string comparison method to use.
        /// </summary>
        StringComparison Comparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        /// <summary>
        /// Find the value of the first item that its text matches the given text exactly.
        /// </summary>
        /// <param name="text">Text to look for.</param>
        /// <returns>Matching item value, or null if not found.</returns>
        public string? FindItemByText(string text)
        {
            if (string.IsNullOrEmpty(text)) { return null; }
            foreach (var item in Items)
            {
                if (string.Equals(GetItemText(item), text, Comparison))
                {
                    return item.Value;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override bool ShouldDisplayItem(ListItem item)
        {
            if (!base.ShouldDisplayItem(item)) { return false; }
            if (!FilterItemsByText || string.IsNullOrEmpty(_filterText)) { return true; }
            return GetItemText(item).Contains(_filterText, Comparison);
        }

        /// <summary>
        /// Called when the text input value changes.
        /// </summary>
        void OnTextChanged()
        {
            // text was changed because selection changed
            if (_syncingTextFromSelection) { return; }

            // select item matching text
            _syncingSelectionFromText = true;
            try
            {
                var match = FindItemByText(Text);
                if (match != null)
                {
                    SelectedValue = match;
                }
                else if (AllowFreeText)
                {
                    SelectedValue = null;
                }
                // note: when free text is not allowed and text doesn't match (or is empty), we keep previous selection until text is committed
            }
            finally
            {
                _syncingSelectionFromText = false;
            }

            // if the user typed the text, filter list and open it
            if (!_settingTextByCode)
            {
                _filterText = Text;
                if (VerticalScrollbar != null)
                {
                    VerticalScrollbar.ValueSafe = 0;
                }
                if (OpenListWhileTyping && !IsOpened)
                {
                    OpenList();
                }
            }

            // trigger events
            RaiseValueChangedIfNeeded();
        }

        /// <inheritdoc/>
        protected override void OnSelectedValueChanged()
        {
            // selection changed not by typing - update text to match selection
            if (!_syncingSelectionFromText)
            {
                SyncTextToSelection();
            }
            RaiseValueChangedIfNeeded();
        }

        /// <summary>
        /// Set text to match currently selected item, or empty if there's no selected item.
        /// </summary>
        void SyncTextToSelection()
        {
            var index = SelectedIndex;
            var text = (index >= 0) ? GetItemText(Items[index]) : string.Empty;
            if (_textInput.Value != text)
            {
                _syncingTextFromSelection = true;
                try
                {
                    _textInput.Value = text;
                }
                finally
                {
                    _syncingTextFromSelection = false;
                }
                _textInput.CaretOffset = text.Length;
                _textInput.ClearSelection();
            }
            RaiseValueChangedIfNeeded();
        }

        /// <summary>
        /// Trigger value changed events, if value changed since last time we triggered them.
        /// </summary>
        void RaiseValueChangedIfNeeded()
        {
            var value = Value;
            if (value != _lastReportedValue)
            {
                _lastReportedValue = value;
                Events.OnValueChanged?.Invoke(this);
                UISystem.Events.OnValueChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Commit the currently typed text.
        /// If free text is not allowed, will clear selection if text is empty, or revert text that doesn't match any item to the selected item text.
        /// If text matches an item, will also normalize text to the exact item text (for example, fix letters case).
        /// </summary>
        public void CommitText()
        {
            if (!AllowFreeText && string.IsNullOrEmpty(Text))
            {
                SelectedValue = null;
            }
            if (!AllowFreeText || (SelectedValue != null))
            {
                SyncTextToSelection();
            }
        }

        /// <summary>
        /// Commit text when the user press enter.
        /// If free text is not allowed and text doesn't match any item, will select the first item that match the filter.
        /// </summary>
        internal void OnEnterPressed()
        {
            if (!AllowFreeText && (FindItemByText(Text) == null) && !string.IsNullOrEmpty(_filterText))
            {
                UpdateDisplayedItems();
                if (DisplayedItemsCount > 0)
                {
                    SelectedIndex = _displayedItemIndices[0];
                }
            }
            CommitText();
            CloseList();
        }

        /// <summary>
        /// Select next / previous displayed item, when using up and down arrows while typing.
        /// </summary>
        /// <param name="direction">-1 to go up, 1 to go down.</param>
        internal void NavigateItems(int direction)
        {
            // open list if needed
            if (!IsOpened)
            {
                OpenList();
            }

            // get index of new item to select
            UpdateDisplayedItems();
            var count = DisplayedItemsCount;
            if (count == 0) { return; }
            var current = _displayedItemIndices.IndexOf(SelectedIndex);
            int next;
            if (current < 0)
            {
                next = (direction > 0) ? 0 : (count - 1);
            }
            else
            {
                next = (current + direction + count) % count;
            }

            // select it (this will also update text)
            SelectedIndex = _displayedItemIndices[next];

            // make sure selected item is visible
            if (VerticalScrollbar != null)
            {
                var visibleItems = Math.Max(1, _paragraphs.Count - 1);
                if (next < VerticalScrollbar.Value)
                {
                    VerticalScrollbar.ValueSafe = next;
                }
                else if (next >= VerticalScrollbar.Value + visibleItems)
                {
                    VerticalScrollbar.ValueSafe = next - visibleItems + 1;
                }
            }
        }

        /// <inheritdoc/>
        public override void CloseList()
        {
            var wasOpened = IsOpened;
            base.CloseList();
            if (wasOpened)
            {
                _filterText = string.Empty;
                CommitText();
            }
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // set text input width to not cover the arrow icon, so clicking the icon will toggle the list
            var availableWidth = SelectedValuePanel.LastInternalBoundingRect.Width;
            if (availableWidth > 0)
            {
                var icon = ArrowIcon;
                var iconWidth = ((icon != null) && icon.Visible) ? icon.LastBoundingRect.Width : 0;
                _textInput.Size.X.SetPixels(Math.Max(1, availableWidth - iconWidth));
            }

            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);
        }

        /// <inheritdoc/>
        internal override void PostUpdate(InputState inputState)
        {
            base.PostUpdate(inputState);

            // commit text when text input loses focus
            var isEditing = _textInput.IsEditing;
            if (_wasEditing && !isEditing)
            {
                CommitText();
            }
            _wasEditing = isEditing;
        }

        /// <summary>
        /// Text input used internally by the combo box.
        /// </summary>
        class ComboBoxTextInput : TextInput
        {
            ComboBox _owner;

            public ComboBoxTextInput(ComboBox owner, UISystem system, StyleSheet? stylesheet) : base(system, stylesheet)
            {
                _owner = owner;
            }

            /// <inheritdoc/>
            internal override void DoInteractions(InputState inputState)
            {
                base.DoInteractions(inputState);

                // open list when clicked
                if (inputState.LeftMousePressedNow && _owner.OpenListOnClick && IsPointedOn(inputState.MousePosition, true))
                {
                    _owner.OpenList();
                }

                // special commands
                if (IsEditing && (inputState.TextInputCommands != null))
                {
                    foreach (var cmd in inputState.TextInputCommands)
                    {
                        switch (cmd)
                        {
                            case Drivers.TextInputCommands.MoveCaretUp:
                                _owner.NavigateItems(-1);
                                break;

                            case Drivers.TextInputCommands.MoveCaretDown:
                                _owner.NavigateItems(1);
                                break;

                            case Drivers.TextInputCommands.BreakLine:
                                _owner.OnEnterPressed();
                                break;
                        }
                    }
                }
            }
        }
    }
}
