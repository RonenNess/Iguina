using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A read-only, scrollable view of text entries (usually lines), that fetches only the visible entries from an <see cref="IVirtualTextProvider"/>.
    /// Useful to show long texts, like log files or big documents, without creating an entity per line or loading everything into the UI.
    /// </summary>
    public class VirtualTextView : Panel
    {
        /// <summary>
        /// The provider to get entries from.
        /// If null, the view will be empty.
        /// </summary>
        public IVirtualTextProvider? Provider;

        /// <summary>
        /// If true and the view is scrolled to the end, it will stay at the end when new entries are added.
        /// Useful for live logs.
        /// </summary>
        public bool AutoScrollToEnd = false;

        /// <summary>
        /// If true, entries text will be parsed for paragraph style commands (like ${FC:FF0000}).
        /// Entries with invalid style commands are shown as plain text, instead of throwing exceptions.
        /// Off by default, since external text (like log files) may contain text that looks like style commands.
        /// </summary>
        public bool EnableStyleCommands = false;

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the entries paragraphs.
        /// </summary>
        public StyleSheetState OverrideEntryStyles = new();

        /// <summary>
        /// Stylesheet used for entries.
        /// </summary>
        public StyleSheet EntriesStyleSheet { get; private set; }

        /// <summary>
        /// The height, in pixels, of a single entry.
        /// </summary>
        public int EntryHeight { get; private set; }

        /// <summary>
        /// Total entries count, as returned by the provider on the last draw (or 0 if there's no provider).
        /// </summary>
        public int EntriesCount { get; private set; }

        /// <summary>
        /// How many entries fully fit in the view, as calculated on the last draw.
        /// </summary>
        public int VisibleEntriesCount { get; private set; }

        /// <summary>
        /// Get / set the index of the first visible entry (the scroll offset).
        /// When set, the value is clamped to the valid range on the next draw.
        /// </summary>
        public int ScrollOffset
        {
            get => _pendingScrollOffset ?? VerticalScrollbar?.Value ?? 0;
            set => _pendingScrollOffset = Math.Max(0, value);
        }

        // scroll offset to apply on next draw, once we know the max scroll value
        int? _pendingScrollOffset;

        // paragraph entities to present the visible entries
        List<Paragraph> _paragraphs = new();

        // for every paragraph, the value of EnableStyleCommands its current text was validated for
        List<bool> _paragraphsStyleCommandsSetting = new();

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <inheritdoc/>
        protected override bool WalkInternalChildren => true;

        /// <summary>
        /// Create the virtual text view.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">View panel stylesheet.</param>
        /// <param name="entriesStylesheet">Entries paragraphs stylesheet. If not set, will use the default paragraphs stylesheet.</param>
        /// <param name="provider">Provider to get entries from.</param>
        public VirtualTextView(UISystem system, StyleSheet? stylesheet, StyleSheet? entriesStylesheet, IVirtualTextProvider? provider = null) : base(system, stylesheet)
        {
            EntriesStyleSheet = entriesStylesheet ?? system.DefaultStylesheets.Paragraphs ?? StyleSheet;
            Provider = provider;

            // set defaults
            OverflowMode = OverflowMode.HideOverflow;

            // calculate entry height
            var paragraph = new Paragraph(system, EntriesStyleSheet);
            if (EntriesStyleSheet.DefaultHeight?.Units == MeasureUnit.Pixels)
            {
                EntryHeight = (int)(EntriesStyleSheet.DefaultHeight.Value.Value);
            }
            else
            {
                EntryHeight = paragraph.MeasureTextLineHeight();
            }
            var es = paragraph.GetExtraSize();
            EntryHeight += Math.Max(paragraph.GetMarginBefore().Y, paragraph.GetMarginAfter().Y) + es.Bottom + es.Top;
            EntryHeight = Math.Max(1, EntryHeight);

            // create scrollbar (we set its max value ourselves)
            CreateVerticalScrollbar(false);
        }

        /// <summary>
        /// Create the virtual text view with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="provider">Provider to get entries from.</param>
        public VirtualTextView(UISystem system, IVirtualTextProvider? provider = null) : this(system, system.DefaultStylesheets.Panels, system.DefaultStylesheets.Paragraphs, provider)
        {
        }

        /// <summary>
        /// Scroll to the first entry.
        /// </summary>
        public void ScrollToStart()
        {
            ScrollOffset = 0;
        }

        /// <summary>
        /// Scroll to the last entry.
        /// </summary>
        public void ScrollToEnd()
        {
            ScrollOffset = int.MaxValue;
        }

        /// <summary>
        /// Scroll so that the given entry is visible.
        /// If the entry is already visible, will not change the scroll offset.
        /// </summary>
        /// <param name="index">Entry index to show.</param>
        public void ScrollToEntry(int index)
        {
            long offset = ScrollOffset;
            long visible = Math.Max(1, VisibleEntriesCount);
            if (index < offset)
            {
                ScrollOffset = index;
            }
            else if (index >= offset + visible)
            {
                ScrollOffset = (int)Math.Max(0, index - visible + 1);
            }
        }

        /// <summary>
        /// Set the height of this view to show exactly this number of entries.
        /// </summary>
        /// <param name="entries">How many entries should be visible.</param>
        public void SetVisibleEntriesCount(int entries)
        {
            AutoHeight = false;
            Size.Y.SetPixels(EntryHeight * Math.Max(entries, 1) + GetNonContentHeight());
        }

        /// <summary>
        /// Get how much of the entity height is not used for entries (padding minus extra size).
        /// </summary>
        int GetNonContentHeight()
        {
            var padding = GetPadding();
            var extra = GetExtraSize();
            return Math.Max(0, padding.Top + padding.Bottom - extra.Top - extra.Bottom);
        }

        /// <inheritdoc/>
        protected override Point GetScrollOffset()
        {
            // scrollbar changes which entries we show, so we don't need offset.
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
                var count = Provider?.GetEntriesCount() ?? 0;
                var height = (int)Math.Min((long)EntryHeight * Math.Max(count, 1) + GetNonContentHeight(), int.MaxValue);
                if (AutoHeightMaxSize.HasValue && height > AutoHeightMaxSize.Value)
                {
                    height = AutoHeightMaxSize.Value;
                }
                Size.Y.SetPixels(height);
            }
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // render base
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // get entries count
            EntriesCount = Math.Max(0, Provider?.GetEntriesCount() ?? 0);

            // calculate how many entries fully fit, and how many paragraphs we need (including a partially visible last entry)
            VisibleEntriesCount = Math.Max(1, internalBoundingRect.Height / EntryHeight);
            int paragraphsCount = (int)Math.Ceiling((float)internalBoundingRect.Height / (float)EntryHeight);
            paragraphsCount = Math.Clamp(paragraphsCount, 0, EntriesCount);

            // create new paragraphs
            while (_paragraphs.Count < paragraphsCount)
            {
                var p = new Paragraph(UISystem, EntriesStyleSheet, string.Empty, false);
                p.TextOverflowMode = TextOverflowMode.Overflow;
                p.EnableStyleCommands = false;
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
                p.Events.OnClick = (Entity entity) => { this.Events.OnClick?.Invoke(this); };

                AddChildInternal(p);
                p.IgnoreScrollOffset = true;
                _paragraphs.Add(p);
                _paragraphsStyleCommandsSetting.Add(false);
            }

            // remove extra paragraphs
            while (_paragraphs.Count > paragraphsCount)
            {
                RemoveChildInternal(_paragraphs[^1]);
                _paragraphs.RemoveAt(_paragraphs.Count - 1);
                _paragraphsStyleCommandsSetting.RemoveAt(_paragraphsStyleCommandsSetting.Count - 1);
            }

            // update scrollbar and get scroll offset
            int scrollOffset = 0;
            if (VerticalScrollbar != null)
            {
                bool wasAtEnd = VerticalScrollbar.Value >= VerticalScrollbar.MaxValue;
                VerticalScrollbar.MaxValue = Math.Max(0, EntriesCount - VisibleEntriesCount);
                VerticalScrollbar.Visible = VerticalScrollbar.MaxValue > 0;

                if (_pendingScrollOffset.HasValue)
                {
                    VerticalScrollbar.ValueSafe = _pendingScrollOffset.Value;
                    _pendingScrollOffset = null;
                }
                else if (AutoScrollToEnd && wasAtEnd)
                {
                    VerticalScrollbar.Value = VerticalScrollbar.MaxValue;
                }
                scrollOffset = VerticalScrollbar.Value;
            }

            // set paragraphs text
            for (int i = 0; i < _paragraphs.Count; ++i)
            {
                var paragraph = _paragraphs[i];
                var entryIndex = scrollOffset + i;
                if (entryIndex >= EntriesCount)
                {
                    paragraph.Visible = false;
                    continue;
                }
                paragraph.Visible = true;
                paragraph.UserData = entryIndex;
                paragraph.OverrideStyles = OverrideEntryStyles;

                // set text. if style commands are enabled, validate them when text changes, and show entries with invalid commands as plain text instead of throwing
                var text = SanitizeEntry(Provider!.GetEntry(entryIndex));
                if ((paragraph.Text != text) || (_paragraphsStyleCommandsSetting[i] != EnableStyleCommands))
                {
                    paragraph.Text = text;
                    paragraph.EnableStyleCommands = EnableStyleCommands && Paragraph.AreStyleCommandsValid(text);
                    _paragraphsStyleCommandsSetting[i] = EnableStyleCommands;
                }
            }
        }

        /// <summary>
        /// Make sure an entry is a single line, since entries are drawn as single lines.
        /// </summary>
        static string SanitizeEntry(string? entry)
        {
            if (string.IsNullOrEmpty(entry)) { return string.Empty; }
            if (entry.Contains('\n') || entry.Contains('\r'))
            {
                return entry.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
            }
            return entry;
        }

        /// <inheritdoc/>
        internal override void DoFocusedEntityInteractions(InputState inputState)
        {
            // call base class to trigger events
            base.DoFocusedEntityInteractions(inputState);

            // scroll with up / down keys
            if (VerticalScrollbar != null)
            {
                if (inputState.KeyboardInteraction == Drivers.KeyboardInteractions.MoveUp)
                {
                    VerticalScrollbar.ValueSafe = VerticalScrollbar.Value - 1;
                }
                if (inputState.KeyboardInteraction == Drivers.KeyboardInteractions.MoveDown)
                {
                    VerticalScrollbar.ValueSafe = VerticalScrollbar.Value + 1;
                }
            }
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
