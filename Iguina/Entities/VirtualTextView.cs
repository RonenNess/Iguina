using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A read-only, scrollable view of text entries (usually lines), that fetches only the visible entries from an <see cref="IVirtualTextProvider"/>.
    /// Useful to show long texts, like log files or big documents, without creating an entity per line or loading everything into the UI.
    /// </summary>
    /// <remarks>
    /// By default every entry is a single line, and all entries have the same height (<see cref="EntryHeight"/>).
    /// If <see cref="EntriesOverflowMode"/> is set to wrap, or <see cref="AllowLineBreaks"/> is true, entries may take multiple lines.
    /// Scrolling is always done in whole entries.
    /// </remarks>
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
        /// How to handle entries that are wider than the view.
        /// Overflow (default) keeps every entry on a single line and cuts it at the view edge.
        /// WrapWords / WrapImmediate break long entries into multiple lines.
        /// </summary>
        public TextOverflowMode EntriesOverflowMode = TextOverflowMode.Overflow;

        /// <summary>
        /// If true, line breaks inside entries are kept and the entry is shown as multiple lines.
        /// If false (default), line breaks are replaced with spaces.
        /// </summary>
        public bool AllowLineBreaks = false;

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the entries paragraphs.
        /// </summary>
        public StyleSheetState OverrideEntryStyles = new();

        /// <summary>
        /// Stylesheet used for entries.
        /// </summary>
        public StyleSheet EntriesStyleSheet { get; private set; }

        /// <summary>
        /// The height, in pixels, of a single-line entry, including its margins.
        /// This is the exact distance between drawn single-line entries.
        /// Calculated from the entries stylesheet merged with <see cref="OverrideEntryStyles"/>, so it's always up-to-date.
        /// </summary>
        public int EntryHeight
        {
            get
            {
                UpdateEntryMetrics();
                return _entryHeight;
            }
        }
        int _entryHeight = 1;

        /// <summary>
        /// The width, in pixels, available for entries text, as calculated on the last draw.
        /// Text measured with the entries style that is not wider than this value is drawn without being cut, and without wrapping.
        /// </summary>
        public int EntryTextWidth { get; private set; }

        /// <summary>
        /// Total entries count, as returned by the provider on the last draw (or 0 if there's no provider).
        /// </summary>
        public int EntriesCount { get; private set; }

        /// <summary>
        /// How many entries fully fit in the view, as calculated on the last draw.
        /// When entries can take multiple lines, this is how many entries fully fit from the current scroll offset.
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

        // paragraph used to resolve entries style and measure entries, without drawing it
        readonly Paragraph _probe;

        // entries metrics, calculated from effective entries style
        int _lineHeight = 1;
        int _minBoxHeight;
        int _entryChrome;
        int _entryTopOffset;
        Sides _entryExtraSize;
        Sides _entryPadding;

        // width to set for entries paragraphs, and left offset for them (when scrollbar is on the left side)
        int _paragraphWidth = 1;
        int _paragraphOffsetX;

        // view internal height on last draw
        int _lastViewHeight;

        // cache of lines count per entry, used when entries can take multiple lines
        readonly Dictionary<int, (string Text, int Width, int Lines)> _linesCountCache = new();
        object? _linesCountCacheKey;
        const int MaxLinesCountCacheSize = 10000;

        /// <summary>
        /// True if entries may take more than one line.
        /// </summary>
        bool IsVariableHeight => AllowLineBreaks || (EntriesOverflowMode != TextOverflowMode.Overflow);

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

            // create the paragraph we use to resolve style and measure entries
            _probe = new Paragraph(system, EntriesStyleSheet, string.Empty, false);
            _probe.LockedState = EntityState.Default;
            UpdateEntryMetrics();

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
        /// Calculate entry metrics from the effective entries style (stylesheet + overrides).
        /// This is the single source of truth for the vertical space an entry takes, used both for layout math and for drawing.
        /// </summary>
        void UpdateEntryMetrics()
        {
            _probe.OverrideStyles = OverrideEntryStyles;
            _lineHeight = Math.Max(1, _probe.MeasureTextLineHeight());
            _minBoxHeight = (EntriesStyleSheet.DefaultHeight?.Units == MeasureUnit.Pixels) ? (int)EntriesStyleSheet.DefaultHeight.Value.Value : 0;

            var marginBefore = _probe.GetMarginBefore();
            var marginAfter = _probe.GetMarginAfter();
            _entryExtraSize = _probe.GetExtraSize();
            _entryPadding = _probe.GetPadding();

            // an entry occupies: margin before, extra size top, text box, extra size bottom, margin after
            _entryChrome = marginBefore.Y + marginAfter.Y + _entryExtraSize.Top + _entryExtraSize.Bottom;
            _entryTopOffset = marginBefore.Y + _entryExtraSize.Top;
            _entryHeight = GetEntryHeightForLines(1);
        }

        /// <summary>
        /// Get the text box height (without margins and extra size) of an entry with a given lines count.
        /// </summary>
        int GetBoxHeightForLines(int lines)
        {
            return Math.Max(lines * _lineHeight, _minBoxHeight);
        }

        /// <summary>
        /// Get the full height of an entry (including margins and extra size) with a given lines count.
        /// </summary>
        int GetEntryHeightForLines(int lines)
        {
            return Math.Max(1, GetBoxHeightForLines(lines) + _entryChrome);
        }

        /// <summary>
        /// Set the width available for entries (view internal width, minus scrollbar if visible), and calculate entries text width from it.
        /// </summary>
        void SetAvailableWidth(int availableWidth)
        {
            // note: paragraphs shrink parent rect by 12 pixels (see Paragraph.ProcessParentInternalRect)
            _paragraphWidth = Math.Max(1, availableWidth - 12);
            EntryTextWidth = Math.Max(1, _paragraphWidth + _entryExtraSize.Left + _entryExtraSize.Right - _entryPadding.Left - _entryPadding.Right);
        }

        /// <summary>
        /// Is the given anchor on the left side.
        /// </summary>
        static bool IsLeftAnchor(Anchor anchor) => (anchor == Anchor.TopLeft) || (anchor == Anchor.CenterLeft) || (anchor == Anchor.BottomLeft);

        /// <summary>
        /// Get how much width the vertical scrollbar takes when visible.
        /// Works even if the scrollbar was never drawn yet.
        /// </summary>
        int GetScrollbarReservedWidth(Rectangle internalRect)
        {
            var scrollbar = VerticalScrollbar;
            if (scrollbar == null) { return 0; }
            int width = scrollbar.LastBoundingRect.Width;
            if (width <= 0)
            {
                var extra = scrollbar.GetExtraSize();
                width = scrollbar.Size.X.GetValueInPixels(internalRect.Width) + extra.Left + extra.Right;
            }
            var margin = IsLeftAnchor(scrollbar.Anchor) ? scrollbar.GetMarginAfter().X : scrollbar.GetMarginBefore().X;
            return Math.Max(0, width + margin);
        }

        /// <summary>
        /// Make sure an entry is valid for presentation.
        /// </summary>
        string SanitizeEntry(string? entry)
        {
            if (string.IsNullOrEmpty(entry)) { return string.Empty; }
            if (!AllowLineBreaks && (entry.Contains('\n') || entry.Contains('\r')))
            {
                return entry.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
            }
            return entry;
        }

        /// <summary>
        /// Clear lines count cache if anything that affects lines count (other than text and width) changed.
        /// </summary>
        void ValidateLinesCountCache()
        {
            var state = _probe.State;
            var key = (
                EntriesStyleSheet.GetProperty<string?>("FontIdentifier", state, null, OverrideEntryStyles),
                EntriesStyleSheet.GetProperty<int>("FontSize", state, 24, OverrideEntryStyles),
                EntriesStyleSheet.GetProperty<float>("TextScale", state, 1f, OverrideEntryStyles),
                EntriesStyleSheet.GetProperty<float>("TextSpacing", state, 1f, OverrideEntryStyles),
                UISystem.TextsScale,
                EntriesOverflowMode,
                AllowLineBreaks,
                EnableStyleCommands);
            if (!key.Equals(_linesCountCacheKey))
            {
                _linesCountCache.Clear();
                _linesCountCacheKey = key;
            }
        }

        /// <summary>
        /// Get how many lines a given entry takes, at the current entries text width.
        /// </summary>
        int GetEntryLines(int index, string text)
        {
            if (!IsVariableHeight) { return 1; }

            // get from cache
            if (_linesCountCache.TryGetValue(index, out var cached) && (cached.Width == EntryTextWidth) && (ReferenceEquals(cached.Text, text) || (cached.Text == text)))
            {
                return cached.Lines;
            }

            // measure, using the same text processing paragraphs use for drawing
            _probe.TextOverflowMode = EntriesOverflowMode;
            _probe.Text = text;
            _probe.EnableStyleCommands = EnableStyleCommands && Paragraph.AreStyleCommandsValid(text);
            var lines = _probe.CountTextLines(EntryTextWidth);

            // add to cache
            if (_linesCountCache.Count >= MaxLinesCountCacheSize) { _linesCountCache.Clear(); }
            _linesCountCache[index] = (text, EntryTextWidth, lines);
            return lines;
        }

        /// <summary>
        /// Get the full height of an entry by index, at the current entries text width.
        /// </summary>
        int GetEntryHeight(IVirtualTextProvider provider, int index)
        {
            if (!IsVariableHeight) { return _entryHeight; }
            return GetEntryHeightForLines(GetEntryLines(index, SanitizeEntry(provider.GetEntry(index))));
        }

        /// <summary>
        /// Get the smallest index to start drawing from so that all entries up to (and including) the given entry fully fit in the given height.
        /// If the given entry alone doesn't fit, returns its index.
        /// </summary>
        int GetFirstEntryToFit(IVirtualTextProvider provider, int lastIndex, int height)
        {
            long y = GetEntryHeight(provider, lastIndex);
            int first = lastIndex;
            while (first > 0)
            {
                var h = GetEntryHeight(provider, first - 1);
                if (y + h > height) { break; }
                y += h;
                first--;
            }
            return first;
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
                // entries with variable height: find the first entry so that the requested entry is at the bottom
                var provider = Provider;
                int count = provider?.GetEntriesCount() ?? 0;
                if (IsVariableHeight && (provider != null) && (count > 0) && (_lastViewHeight > 0))
                {
                    UpdateEntryMetrics();
                    ValidateLinesCountCache();
                    ScrollOffset = GetFirstEntryToFit(provider, Math.Min(index, count - 1), _lastViewHeight);
                }
                else
                {
                    ScrollOffset = (int)Math.Max(0, index - visible + 1);
                }
            }
        }

        /// <summary>
        /// Set the height of this view to show exactly this number of single-line entries.
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
                UpdateEntryMetrics();
                var provider = Provider;
                var count = Math.Max(0, provider?.GetEntriesCount() ?? 0);
                long contentHeight;

                // variable height entries: measure entries, up to max size (or a limit, after which we estimate)
                if (IsVariableHeight && (provider != null) && (count > 0) && (_lastViewHeight > 0))
                {
                    ValidateLinesCountCache();
                    const int MaxEntriesToMeasure = 1000;
                    long limit = AutoHeightMaxSize ?? long.MaxValue;
                    contentHeight = 0;
                    int measured = 0;
                    while ((measured < count) && (measured < MaxEntriesToMeasure) && (contentHeight <= limit))
                    {
                        contentHeight += GetEntryHeight(provider, measured);
                        measured++;
                    }
                    if ((measured < count) && (contentHeight <= limit))
                    {
                        contentHeight += (long)_entryHeight * (count - measured);
                    }
                }
                // single-line entries
                else
                {
                    contentHeight = (long)_entryHeight * Math.Max(count, 1);
                }

                var height = (int)Math.Min(contentHeight + GetNonContentHeight(), int.MaxValue);
                if (AutoHeightMaxSize.HasValue && height > AutoHeightMaxSize.Value)
                {
                    height = AutoHeightMaxSize.Value;
                }
                Size.Y.SetPixels(height);
            }
        }

        /// <summary>
        /// Create a new paragraph to present entries.
        /// </summary>
        Paragraph CreateEntryParagraph()
        {
            var p = new Paragraph(UISystem, EntriesStyleSheet, string.Empty, false);
            p.TextOverflowMode = TextOverflowMode.Overflow;
            p.EnableStyleCommands = false;
            p.ShrinkToMinimalSize = false;
            p.PassFocusTo = this;
            p._overrideInteractableState = true;
            p.Anchor = Anchor.TopLeft;

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
            return p;
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // render base
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // update entry metrics from effective style (may change at runtime via overrides, stylesheet or texts scale)
            UpdateEntryMetrics();
            bool variableHeight = IsVariableHeight;
            if (variableHeight) { ValidateLinesCountCache(); }

            // get entries count
            var provider = Provider;
            EntriesCount = Math.Max(0, provider?.GetEntriesCount() ?? 0);
            int viewHeight = Math.Max(0, internalBoundingRect.Height);
            _lastViewHeight = viewHeight;

            // calculate max scroll offset and if we need a scrollbar.
            // first try with full width, and if scrollbar is needed recalculate with the width that remains next to it.
            int fixedVisibleCount = Math.Max(1, viewHeight / _entryHeight);
            SetAvailableWidth(internalBoundingRect.Width);
            int CalculateMaxOffset()
            {
                if (EntriesCount == 0) { return 0; }
                if (!variableHeight) { return Math.Max(0, EntriesCount - fixedVisibleCount); }
                return GetFirstEntryToFit(provider!, EntriesCount - 1, viewHeight);
            }
            int maxOffset = CalculateMaxOffset();
            bool showScrollbar = (maxOffset > 0) && (VerticalScrollbar != null);
            _paragraphOffsetX = 0;
            if (showScrollbar)
            {
                var scrollbarWidth = GetScrollbarReservedWidth(internalBoundingRect);
                if (IsLeftAnchor(VerticalScrollbar!.Anchor)) { _paragraphOffsetX = scrollbarWidth; }
                SetAvailableWidth(internalBoundingRect.Width - scrollbarWidth);
                if (variableHeight) { maxOffset = CalculateMaxOffset(); }
            }

            // update scrollbar and get scroll offset
            int scrollOffset = 0;
            if (VerticalScrollbar != null)
            {
                bool wasAtEnd = VerticalScrollbar.Value >= VerticalScrollbar.MaxValue;
                VerticalScrollbar.MaxValue = maxOffset;
                VerticalScrollbar.Visible = showScrollbar;

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

            // set paragraphs text and position, until we fill the view.
            // every entry gets exactly the height used for layout math above, so drawing and scrolling always agree.
            int y = 0;
            int used = 0;
            int fullyVisible = 0;
            for (long entryIndex = scrollOffset; (entryIndex < EntriesCount) && (y < viewHeight); ++entryIndex, ++used)
            {
                // get or create paragraph
                if (used >= _paragraphs.Count)
                {
                    _paragraphs.Add(CreateEntryParagraph());
                    _paragraphsStyleCommandsSetting.Add(false);
                }
                var paragraph = _paragraphs[used];
                paragraph.Visible = true;
                paragraph.UserData = (int)entryIndex;
                paragraph.OverrideStyles = OverrideEntryStyles;
                paragraph.TextOverflowMode = EntriesOverflowMode;

                // set text. if style commands are enabled, validate them when text changes, and show entries with invalid commands as plain text instead of throwing
                var text = SanitizeEntry(provider!.GetEntry((int)entryIndex));
                if ((paragraph.Text != text) || (_paragraphsStyleCommandsSetting[used] != EnableStyleCommands))
                {
                    paragraph.Text = text;
                    paragraph.EnableStyleCommands = EnableStyleCommands && Paragraph.AreStyleCommandsValid(text);
                    _paragraphsStyleCommandsSetting[used] = EnableStyleCommands;
                }

                // calculate entry height and place paragraph
                int lines = GetEntryLines((int)entryIndex, text);
                int entryHeight = GetEntryHeightForLines(lines);
                paragraph._textMaxWidthOverride = EntryTextWidth;
                paragraph.Size.X.SetPixels(_paragraphWidth);
                paragraph.Size.Y.SetPixels(GetBoxHeightForLines(lines));
                paragraph.Offset.X.SetPixels(_paragraphOffsetX);
                paragraph.Offset.Y.SetPixels(y + _entryTopOffset);

                // count fully visible entries and advance
                if (y + entryHeight <= viewHeight) { fullyVisible++; }
                y += entryHeight;
            }

            // hide unused paragraphs, and remove paragraphs we can never need with current height
            int maxParagraphs = (int)Math.Ceiling((float)viewHeight / (float)_entryHeight) + 1;
            while (_paragraphs.Count > Math.Max(used, maxParagraphs))
            {
                RemoveChildInternal(_paragraphs[^1]);
                _paragraphs.RemoveAt(_paragraphs.Count - 1);
                _paragraphsStyleCommandsSetting.RemoveAt(_paragraphsStyleCommandsSetting.Count - 1);
            }
            for (int i = used; i < _paragraphs.Count; ++i)
            {
                _paragraphs[i].Visible = false;
            }

            // set visible entries count
            VisibleEntriesCount = variableHeight ? Math.Max(1, fullyVisible) : fixedVisibleCount;
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
