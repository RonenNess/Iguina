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
            public string? Label { get; internal set; }
        }

        // list items
        List<ListItem> _items = new();

        // paragraph entities to present the items
        protected List<Paragraph> _paragraphs = new();

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
        /// Get selected item text.
        /// Can be the item label, if a label is defined, or the value itself if not.
        /// Will return null if no value is selected.
        /// </summary>
        public string? SelectedText
        {
            get
            {
                if (SelectedValue == null) { return null; }
                return _items[SelectedIndex].Label ?? SelectedValue;
            }
        }

        /// <summary>
        /// If true and user clicks on the selected item, it will deselect it.
        /// </summary>
        public bool AllowDeselect = true;

        // stylesheet for items
        StyleSheet? _itemsStylesheet;

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

        /// <summary>
        /// Create the list box.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">List box panel stylesheet.</param>
        /// <param name="itemsStylesheet">List box items stylesheet. If not set, will use the same as base stylesheet.</param>
        public ListBox(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null) : base(system, stylesheet) 
        {
            _itemsStylesheet = itemsStylesheet ?? StyleSheet;

            // set defaults
            OverflowMode = OverflowMode.HideOverflow;

            // create paragraph to calculate item height
            var paragraph = new Paragraph(system, _itemsStylesheet);
            if (_itemsStylesheet.DefaultHeight?.Units == MeasureUnit.Pixels)
            {
                ItemHeight = (int)(_itemsStylesheet.DefaultHeight.Value.Value);
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

        /// <inheritdoc/>
        protected override Point GetScrollOffset()
        {
            // scrollbar change list values, so we don't need offset.
            return Point.Zero;
        }

        /// <inheritdoc/>
        protected override void SetAutoSizes(int maxWidth, int maxHeight)
        {
            // set auto size
            if (AutoWidth)
            {
                Size.X.SetPixels(maxWidth);
            }
            if (AutoHeight)
            {
                Size.Y.SetPixels(ItemHeight * (_items.Count + 1));
            }
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // render base
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // calculate how many items we should show at any given time
            int paragraphsCount = (int)Math.Ceiling((float)internalBoundingRect.Height / (float)ItemHeight);
            if (paragraphsCount > _items.Count) { paragraphsCount = _items.Count; }

            // create new item paragraphs
            while (_paragraphs.Count < paragraphsCount)
            {
                var p = new Paragraph(UISystem, _itemsStylesheet, "", false);
                p.TextOverflowMode = TextOverflowMode.Overflow;
                p.ShrinkWidthToMinimalSize = false;
                p._overrideInteractableState = true;
                p.Size.X.SetPercents(100f);
                
                // selecting list item value
                p.Events.OnClick = (Entity entity) =>
                {
                    this.OnItemClicked(entity);
                };
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
                VerticalScrollbar.Visible = ShowListScrollbar;
                VerticalScrollbar.MaxValue = (_items.Count - _paragraphs.Count) + 1;
                scrollOffset = VerticalScrollbar.Visible ? VerticalScrollbar.Value : 0;
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
            if (AllowDeselect && (newValue == SelectedValue))
            {
                SelectedValue = null;
            }
            else
            {
                SelectedValue = newValue;
            }
        }

        /// <summary>
        /// Set the values and texts of the paragraphs.
        /// </summary>
        protected virtual void SetParagraphs(int scrollOffset)
        {
            for (var i = 0; i < _paragraphs.Count; ++i)
            {
                if (i + scrollOffset >= _items.Count)
                {
                    _paragraphs[i].Visible = false;
                    break;
                }
                var item = _items[i + scrollOffset];
                var paragraph = _paragraphs[i];
                paragraph.Visible = true;
                paragraph.UserData = item.Value;
                paragraph.Text = item.Label ?? item.Value;
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
            var item = new ListItem() { Value = value, Label = label };
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
            _items[index] = new ListItem() { Value = value, Label = label };
        }

        /// <summary>
        /// Add item to list.
        /// </summary>
        /// <param name="valueToReplace">Value to replace.</param>
        /// <param name="value">Item unique value.</param>
        /// <param name="label">Item text to show (or null to show value instead).</param>
        /// <param name="index">Index to add this item to.</param>
        public void ReplaceItem(string valueToReplace, string value, string? label = null)
        {
            var index = GetIndexOfValue(valueToReplace);
            if (index < 0 || index >= _items.Count) { throw new IndexOutOfRangeException("Invalid value to replace!"); }
            if (_selectedValue == valueToReplace)
            {
                _selectedValue = value;
            }
            _items[index] = new ListItem() { Value = value, Label = label };
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
