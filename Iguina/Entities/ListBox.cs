using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A list of items users can select items from.
    /// </summary>
    public class ListBox : Panel
    {
        /// <summary>
        /// List item entry.
        /// </summary>
        public struct ListItem
        {
            /// <summary>
            /// List item value.
            /// </summary>
            public string Value { get; internal set; }

            /// <summary>
            /// List item label to show when rendering the list.
            /// If null, will use 'Value' instead.
            /// </summary>
            public string? LabelWithIcon { get; internal set; }

            /// <summary>
            /// List item label text only, without icon codes.
            /// </summary>
            public string? LabelTextOnly { get; internal set; }
        }

        // list items
        List<ListItem> _items = new();

        // paragraph entities to present the items
        protected List<Paragraph> _paragraphs = new();

        /// <summary>
        /// An optional method to filter which values to show in the list.
        /// Return true to show current item, false to hide it.
        /// Regardless of the display filter, the item will still be a part of the list and you can set it as selected via code.
        /// </summary>
        public Func<ListItem, bool>? DisplayFilter;

        /// <summary>
        /// Get list items count.
        /// </summary>
        public int ItemsCount => _items.Count;

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the list items paragraphs.
        /// </summary>
        public StyleSheetState OverrideItemStyles = new();

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the selected item paragraphs.
        /// </summary>
        public StyleSheetState OverrideSelectedItemStyles = new();

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for a specific list item by value.
        /// </summary>
        public Dictionary<string, StyleSheetState> OverrideItemStyleByValue = new();

        /// <summary>
        /// Get / set selected index.
        /// Set to -1 to unselect.
        /// </summary>
        public int SelectedIndex
        {
            get => (SelectedValue != null) ? _items.FindIndex(x => x.Value == SelectedValue) : -1;
            set
            {
                if (value == -1)
                {
                    SelectedValue = null;
                }
                else
                {
                    if (value < 0) throw new IndexOutOfRangeException("Invalid list selected index!");
                    if (value >= _items.Count) throw new IndexOutOfRangeException("Invalid list selected index!");
                    SelectedValue = _items[value].Value;
                }
            }
        }

        /// <summary>
        /// Get / set selected value.
        /// </summary>
        public string? SelectedValue
        {
            get => _selectedValue;
            set
            {
                if (value != _selectedValue)
                {
                    if (value != null && !_items.Any(x => x.Value == value))
                    {
                        throw new KeyNotFoundException("Value to set was not found in list!");
                    }
                    _selectedValue = value;
                    Events.OnValueChanged?.Invoke(this);
                    UISystem.Events.OnValueChanged?.Invoke(this);
                }
            }
        }
        string? _selectedValue;

        /// <summary>
        /// Get selected item text, without any icons.
        /// </summary>
        public string? SelectedText
        {
            get
            {
                if (SelectedValue == null) { return null; }
                return _items[SelectedIndex].LabelTextOnly ?? SelectedValue;
            }
        }

        /// <summary>
        /// Get selected item text, with icons if any are set.
        /// </summary>
        public string? SelectedTextWithIcon
        {
            get
            {
                if (SelectedValue == null) { return null; }
                return _items[SelectedIndex].LabelWithIcon ?? SelectedValue;
            }
        }

        /// <summary>
        /// If true and user clicks on the selected item, it will deselect it.
        /// </summary>
        public bool AllowDeselect = true;

        /// <summary>
        /// Stylesheet used for list items.
        /// </summary>
        public StyleSheet? ItemsStyleSheet { get; private set; }

        /// <summary>
        /// The height of a single item in list.
        /// </summary>
        protected int ItemHeight { get; private set; }

        /// <inheritdoc/>        
        protected override bool WalkInternalChildren => true;

        /// <summary>
        /// Should we show scrollbar for this list?
        /// </summary>
        protected virtual bool ShowListScrollbar => (_paragraphs.Count < _items.Count);

        // Flag to track if scrollbar visibility was manually set
        private bool _manualScrollbarVisibilitySet = false;

        /// <summary>
        /// Create the list box.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">List box panel stylesheet.</param>
        /// <param name="itemsStylesheet">List box items stylesheet. If not set, will use the same as base stylesheet.</param>
        public ListBox(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null) : base(system, stylesheet)
        {
            ItemsStyleSheet = itemsStylesheet ?? StyleSheet;

            // set defaults
            OverflowMode = OverflowMode.HideOverflow;

            // create paragraph to calculate item height
            var paragraph = new Paragraph(system, ItemsStyleSheet);
            if (ItemsStyleSheet.DefaultHeight?.Units == MeasureUnit.Pixels)
            {
                ItemHeight = (int)(ItemsStyleSheet.DefaultHeight.Value.Value);
            }
            else
            {
                ItemHeight = paragraph.MeasureTextLineHeight();
            }
            var es = paragraph.GetExtraSize();
            ItemHeight += Math.Max(paragraph.GetMarginBefore().Y, paragraph.GetMarginAfter().Y) + es.Bottom + es.Top;

            // create scrollbar
            CreateVerticalScrollbar();
        }

        /// <summary>
        /// Create the list box with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public ListBox(UISystem system) : this(system,
            system.DefaultStylesheets.ListPanels ?? system.DefaultStylesheets.Panels,
            system.DefaultStylesheets.ListItems ?? system.DefaultStylesheets.Paragraphs)
        {
        }

        /// <summary>
        /// Override the scrollbar's Visible property to allow direct control.
        /// </summary>
        public void SetScrollbarVisible(bool visible)
        {
            if (VerticalScrollbar != null)
            {
                _manualScrollbarVisibilitySet = true;
                VerticalScrollbar.Visible = visible;
            }
        }

        /// <inheritdoc/>
        protected override Point GetScrollOffset()
        {
            // scrollbar change list values, so we don't need offset.
            return Point.Zero;
        }

        /// <inheritdoc/>
        protected override void SetAutoSizes(int maxWidth, int maxHeight)
        {
            // set auto width
            if (AutoWidth)
            {
                if (AutoWidthMaxSize.HasValue && maxWidth > AutoWidthMaxSize.Value)
                {
                    maxWidth = AutoWidthMaxSize.Value;
                }
                Size.X.SetPixels(maxWidth);
            }

            // set auto height
            if (AutoHeight)
            {
                var height = ItemHeight * (_items.Count + 1);
                if (AutoHeightMaxSize.HasValue && height > AutoHeightMaxSize.Value)
                {
                    height = AutoHeightMaxSize.Value;
                }
                Size.Y.SetPixels(height);
            }
        }

        /// <summary>
        /// Get extra paragraphs count for this list.
        /// </summary>
        protected virtual int GetExtraParagraphsCount()
        {
            return 0;
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // render base
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // calculate how many items we should show at any given time
            int paragraphsCount = (int)Math.Ceiling((float)internalBoundingRect.Height / (float)ItemHeight) + GetExtraParagraphsCount();
            if (paragraphsCount < 1) { paragraphsCount = 1; }
            if (paragraphsCount > _items.Count) { paragraphsCount = _items.Count; }

            // create new item paragraphs
            while (_paragraphs.Count < paragraphsCount)
            {
                // create item paragraph
                var p = new Paragraph(UISystem, ItemsStyleSheet ?? UISystem.DefaultStylesheets.Paragraphs, "", false);
                p.TextOverflowMode = TextOverflowMode.Overflow;
                p.ShrinkWidthToMinimalSize = false;
                p.PassFocusTo = this;
                p._overrideInteractableState = true;
                p.Size.X.SetPercents(100f);

                // pass all events from paragraph to self
                p.Events.OnMouseWheelScrollUp = (Entity entity) => { this.Events.OnMouseWheelScrollUp?.Invoke(this); };
                p.Events.OnMouseWheelScrollDown = (Entity entity) => { this.Events.OnMouseWheelScrollDown?.Invoke(this); };
                p.Events.OnLeftMouseDown = (Entity entity) => { this.Events.OnLeftMouseDown?.Invoke(this); };
                p.Events.OnLeftMousePressed = (Entity entity) => { this.Events.OnLeftMousePressed?.Invoke(this); };
                p.Events.OnLeftMouseReleased = (Entity entity) => { this.Events.OnLeftMouseReleased?.Invoke(this); };
                p.Events.OnRightMouseDown = (Entity entity) => { this.Events.OnRightMouseDown?.Invoke(this); };
                p.Events.OnRightMousePressed = (Entity entity) => { this.Events.OnRightMousePressed?.Invoke(this); };
                p.Events.OnRightMouseReleased = (Entity entity) => { this.Events.OnRightMouseReleased?.Invoke(this); };
                p.Events.WhileMouseHover = (Entity entity) => { this.Events.WhileMouseHover?.Invoke(this); };

                // add click event to select item
                p.Events.OnClick = (Entity entity) =>
                {
                    // select list item
                    this.OnItemClicked(entity);

                    // pass click to self
                    this.Events.OnClick?.Invoke(this);
                };

                // add new paragraph
                AddChildInternal(p);
                p.IgnoreScrollOffset = true;
                _paragraphs.Add(p);
            }
            // remove item paragraphs
            while (_paragraphs.Count > paragraphsCount)
            {
                RemoveChildInternal(_paragraphs[0]);
                _paragraphs.RemoveAt(0);
            }

            // scrollbar offset
            int scrollOffset = 0;

            // show / hide scrollbar
            if (VerticalScrollbar != null)
            {
                // Only set scrollbar visibility based on ShowListScrollbar if not manually set
                if (!_manualScrollbarVisibilitySet)
                {
                    VerticalScrollbar.Visible = ShowListScrollbar;
                }
                VerticalScrollbar.MaxValue = (_items.Count - _paragraphs.Count) + 1;

                // Always use the value from the scrollbar regardless of visibility
                scrollOffset = VerticalScrollbar.Value;
            }

            // set paragraphs values
            SetParagraphs(scrollOffset);
        }

        /// <summary>
        /// Action to perform when a list item is clicked on.
        /// </summary>
        protected virtual void OnItemClicked(Entity entity)
        {
            var newValue = entity.UserData as string;
            if (newValue != null)
            {
                if (AllowDeselect && (newValue == SelectedValue))
                {
                    SelectedValue = null;
                }
                else
                {
                    SelectedValue = newValue;
                }
            }
        }

        /// <summary>
        /// Set the height of this list to show exactly this number of items.
        /// </summary>
        /// <param name="items">How many items should be visible.</param>
        public virtual void SetVisibleItemsCount(int items)
        {
            AutoHeight = false;
            Size.Y.SetPixels(ItemHeight * items);
        }

        /// <summary>
        /// Set the values and texts of the paragraphs.
        /// </summary>
        protected virtual void SetParagraphs(int scrollOffset, int startIndex = 0)
        {
            int skipped = 0;
            for (var i = startIndex; i < _paragraphs.Count; ++i)
            {
                // get item index and validate range
                var itemIndex = i + scrollOffset - startIndex + skipped;
                if (itemIndex < 0) { continue; }
                if (itemIndex >= _items.Count)
                {
                    _paragraphs[i].Visible = false;
                    continue;
                }

                // get current item and apply filter
                var item = _items[itemIndex];
                if (DisplayFilter?.Invoke(item) == false)
                {
                    i--;
                    skipped++;
                    continue;
                }

                // set current paragraph
                var paragraph = _paragraphs[i];
                paragraph.Visible = true;
                paragraph.UserData = item.Value;
                paragraph.Text = item.LabelWithIcon ?? item.Value;
                bool selected = item.Value == SelectedValue;
                paragraph.LockedState = selected ? EntityState.Checked : null;
                OverrideItemStyleByValue.TryGetValue(item.Value, out var perItemStyleValue);
                paragraph.OverrideStyles = selected ? OverrideSelectedItemStyles : (perItemStyleValue ?? OverrideItemStyles);
            }
        }

        /// <summary>
        /// Add item to list.
        /// </summary>
        /// <param name="value">Item unique value.</param>
        /// <param name="label">Item text to show (or null to show value instead).</param>
        /// <param name="index">Index to add this item to.</param>
        public void AddItem(string value, string? label = null, int? index = null)
        {
            var item = new ListItem() { Value = value, LabelWithIcon = label, LabelTextOnly = label };
            if (index.HasValue)
            {
                _items.Insert(index.Value, item);
            }
            else
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// Scroll to selected item.
        /// </summary>
        public void ScrollToSelected()
        {
            if (VerticalScrollbar != null)
            {
                VerticalScrollbar.ValueSafe = SelectedIndex - _paragraphs.Count / 2;
            }
        }

        /// <inheritdoc/>
        internal override void DoFocusedEntityInteractions(InputState inputState)
        {
            // call base class to trigger events
            base.DoFocusedEntityInteractions(inputState);

            // select items with up
            if (inputState.KeyboardInteraction == Drivers.KeyboardInteractions.MoveUp)
            {
                if (SelectedIndex <= 0)
                {
                    SelectedIndex = ItemsCount - 1;
                    if (VerticalScrollbar != null)
                    {
                        VerticalScrollbar.ValueSafe = VerticalScrollbar.MaxValue;
                    }
                }
                else
                {
                    SelectedIndex--;
                    if (VerticalScrollbar != null && (VerticalScrollbar.Value > SelectedIndex))
                    {
                        VerticalScrollbar.ValueSafe = SelectedIndex;
                    }
                }
            }

            // select items with down
            if (inputState.KeyboardInteraction == Drivers.KeyboardInteractions.MoveDown)
            {
                if ((SelectedIndex == ItemsCount - 1) || (SelectedIndex == -1))
                {
                    SelectedIndex = 0;
                    if (VerticalScrollbar != null)
                    {
                        VerticalScrollbar.ValueSafe = 0;
                    }
                }
                else
                {
                    SelectedIndex++;
                    if (VerticalScrollbar != null && (VerticalScrollbar.Value < SelectedIndex - _paragraphs.Count + 1))
                    {
                        VerticalScrollbar.ValueSafe = SelectedIndex - _paragraphs.Count + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Add item to list.
        /// </summary>
        /// <param name="index">Index to replace.</param>
        /// <param name="value">Item unique value.</param>
        /// <param name="label">Item text to show (or null to show value instead).</param>
        /// <param name="index">Index to add this item to.</param>
        public void ReplaceItem(int index, string value, string? label = null)
        {
            if (index < 0 || index >= _items.Count) { throw new IndexOutOfRangeException("Invalid index to replace!"); }
            if (SelectedIndex == index)
            {
                _selectedValue = value;
            }
            _items[index] = new ListItem() { Value = value, LabelTextOnly = label, LabelWithIcon = label };
        }

        /// <summary>
        /// Add item to list.
        /// </summary>
        /// <param name="valueToReplace">Value to replace.</param>
        /// <param name="value">Item unique value.</param>
        /// <param name="label">Item text to show (or null to show value instead).</param>
        public void ReplaceItem(string valueToReplace, string value, string? label = null)
        {
            var index = GetIndexOfValue(valueToReplace);
            if (index < 0 || index >= _items.Count) { throw new IndexOutOfRangeException("Invalid value to replace!"); }
            if (_selectedValue == valueToReplace)
            {
                _selectedValue = value;
            }
            _items[index] = new ListItem() { Value = value, LabelTextOnly = label, LabelWithIcon = label };
        }

        /// <summary>
        /// Change the label of an item in the list, without changing its value.
        /// </summary>
        /// <param name="valueToSet">Value to set label for (must exist in list).</param>
        /// <param name="label">New label to set, or null to remove label and use the item value as label.</param>
        public void SetItemLabel(string valueToSet, string? label)
        {
            ReplaceItem(valueToSet, valueToSet, label);
        }

        /// <summary>
        /// Change the label of an item in the list, without changing its value, and add icon to the beginning of the label.
        /// </summary>
        /// <param name="valueToSet">Value to set label for (must exist in list).</param>
        /// <param name="label">New label to set, or null to remove label and use the item value as label.</param>
        /// <param name="icon">Icon to set.</param>
        /// <param name="iconUseTextColor">If true, icon will use the same tint color as the text.</param>
        public void SetItemLabel(string valueToSet, string label, IconTexture icon, bool iconUseTextColor)
        {
            // set label + icon
            var iconWidth = icon.SourceRect.Width * icon.TextureScale;
            var tempParagraph = new Paragraph(UISystem, ItemsStyleSheet ?? UISystem.DefaultStylesheets.Paragraphs, "", false);
            var spaceWidth = tempParagraph.MeasureText(" ").X;
            var spacesCount = (int)(Math.Ceiling(iconWidth / spaceWidth) + 1);
            var iconUseTextureColorVal = iconUseTextColor ? "y" : "n";
            SetItemLabel(valueToSet, $"${{ICO:{icon.TextureId ?? UISystem.SystemStyleSheet.DefaultTexture}|{icon.SourceRect.X}|{icon.SourceRect.Y}|{icon.SourceRect.Width}|{icon.SourceRect.Height}|{icon.TextureScale}|{iconUseTextureColorVal}}}" + new string(' ', spacesCount) + label);

            // set just label text
            var index = GetIndexOfValue(valueToSet);
            var itemData = _items[index];
            itemData.LabelTextOnly = label;
            _items[index] = itemData;
        }

        /// <summary>
        /// Change the label of an item in the list, without changing its value, to be an icon + the value itself.
        /// </summary>
        /// <param name="valueToSet">Value to set label for (must exist in list).</param>
        /// <param name="icon">Icon to set.</param>
        /// <param name="iconUseTextColor">If true, icon will use the same tint color as the text.</param>
        public void SetItemLabel(string valueToSet, IconTexture icon, bool iconUseTextColor)
        {
            SetItemLabel(valueToSet, valueToSet, icon, iconUseTextColor);

            // set just label text
            var index = GetIndexOfValue(valueToSet);
            var itemData = _items[index];
            itemData.LabelTextOnly = valueToSet;
            _items[index] = itemData;
        }

        /// <summary>
        /// Get item index, or -1 if not found.
        /// </summary>
        public int GetIndexOfValue(string value)
        {
            return _items.FindIndex(x => x.Value == value);
        }

        /// <summary>
        /// Remove all list items by value.
        /// </summary>
        /// <param name="value">Value of item to remove.</param>
        public void RemoveItem(string value)
        {
            if (SelectedValue == value)
            {
                SelectedValue = null;
            }
            _items.RemoveAll(x => x.Value == value);
        }

        /// <summary>
        /// Remove item by index.
        /// </summary>
        /// <param name="index">Item index to remove.</param>
        public void RemoveItem(int index)
        {
            if (SelectedIndex == index)
            {
                SelectedValue = null;
            }
            _items.RemoveAt(index);
        }

        /// <summary>
        /// Clear list.
        /// </summary>
        public void Clear()
        {
            SelectedIndex = -1;
            _items.Clear();
        }

        /// <inheritdoc/>
        protected override MeasureVector GetDefaultEntityTypeSize()
        {
            var ret = new MeasureVector();
            ret.X.SetPercents(100f);
            ret.Y.SetPixels(400);
            return ret;
        }
    }
}
