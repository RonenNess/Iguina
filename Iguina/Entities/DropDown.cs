using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A list of items users can select items from, that collapses while not being interacted with.
    /// </summary>
    public class DropDown : ListBox
    {
        /// <summary>
        /// Is the dropdown currently opened?
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <inheritdoc/>
        protected override bool ShowListScrollbar => base.ShowListScrollbar && IsOpened;

        /// <summary>
        /// Text to show when no value is selected and dropdown is collapsed.
        /// </summary>
        public string? DefaultSelectedText = null;

        /// <inheritdoc/>
        internal override bool TopMostInteractions => IsOpened;

        /// <summary>
        /// If defined, this will be the text to display when the dropdown is collapsed, regardless of the currently selected item or default text.
        /// </summary>
        public string? OverrideSelectedText = null;

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the paragraph showing the selected value in closed state.
        /// </summary>
        public StyleSheetState OverrideClosedStateTextStyles
        {
            get => _selectedValueParagraph.OverrideStyles;
            set => _selectedValueParagraph.OverrideStyles = value;
        }

        // panel for selected value text
        Panel _selectedValuePanel;
        Paragraph _selectedValueParagraph;

        /// <summary>
        /// Create the drop down.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Drop down panel stylesheet.</param>
        /// <param name="itemsStylesheet">Drop down box items stylesheet. If not set, will use the same as base stylesheet.</param>
        public DropDown(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null) : base(system, stylesheet, itemsStylesheet)
        {
            // set as auto-height by default
            AutoHeight = true;

            // create panel with selected value paragraph
            _selectedValuePanel = new Panel(system, stylesheet);
            _selectedValueParagraph = _selectedValuePanel.AddChild(new Paragraph(system, itemsStylesheet ?? stylesheet, string.Empty, true));
            _selectedValuePanel.Size.X.SetPercents(100f);
            _selectedValuePanel.Size.Y.SetPixels(GetClosedStateHeight());
            _selectedValuePanel.IgnoreScrollOffset = true;
            var padding = GetPadding();
            _selectedValuePanel.Offset.X.Value = -padding.Left;
            _selectedValuePanel.Offset.Y.Value = -padding.Top;
            _selectedValuePanel.OverrideStyles.ExtraSize = new Sides() { Right = padding.Left + padding.Right };
            AddChildInternal(_selectedValuePanel);

            // clicking on selected value panel will open / close dropdown
            _selectedValuePanel.Events.OnClick += (Entity entity) =>
            {
                ToggleList();
            };
        }

        /// <summary>
        /// Create the drop down with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public DropDown(UISystem system) : this(system,
            system.DefaultStylesheets.DropDownPanels ?? system.DefaultStylesheets.ListPanels ?? system.DefaultStylesheets.Panels,
            system.DefaultStylesheets.DropDownItems ?? system.DefaultStylesheets.ListItems ?? system.DefaultStylesheets.Paragraphs)
        {
        }


        /// <summary>
        /// Get the dropdown height, in pixels, when its closed.
        /// </summary>
        /// <returns>Drop down height in pixels when closed.</returns>
        public int GetClosedStateHeight(bool includePadding = true, bool includeExtraSize = true)
        {
            var padding = includePadding ? GetPadding() : Sides.Zero;
            var extra = includeExtraSize ? GetExtraSize() : Sides.Zero;
            return ItemHeight + padding.Top + padding.Bottom + extra.Top + extra.Bottom;
        }

        /// <summary>
        /// Set bounding rectangles size to its closed state.
        /// </summary>
        private void SetSizeToClosedState(ref Rectangle boundingRect, ref Rectangle internalBoundingRect)
        {
            internalBoundingRect.Height = GetClosedStateHeight();
            boundingRect.Height = internalBoundingRect.Height;
        }

        /// <inheritdoc/>
        public override void SetVisibleItemsCount(int items)
        {
            AutoHeight = false;
            Size.Y.SetPixels(ItemHeight * (items + 2));
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // special - if we are rendering in open mode, top most
            if (_isCurrentlyDrawingOpenedListTopMost)
            {
                // draw open list
                base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);
                
                // this part makes sure the top panel border is not hidden
                Rectangle rect = boundingRect;
                rect.Height = GetClosedStateHeight();
                DrawFillTextures(rect);
                return;
            }

            // closed? resize to close state BEFORE rendering
            if (!IsOpened)
            {
                SetSizeToClosedState(ref boundingRect, ref internalBoundingRect);
            }

            // dropdown is opened - render as list top-most
            if (IsOpened)
            {
                // move the scrollbar under the selected value box
                //if (ShowSelectedValueBoxWhenOpened)
                {
                    if (VerticalScrollbar != null)
                    {
                        var extra = GetExtraSize();
                        var scrollbarOffset = (int)((GetClosedStateHeight() - extra.Bottom) * 0.85f);
                        VerticalScrollbar.Offset.Y.Value = scrollbarOffset;
                        VerticalScrollbar.OverrideStyles.ExtraSize = new Sides(0, 0, 0, -scrollbarOffset);
                    }
                }

                // draw opened list in top-most mode at the end of frame
                DrawMethodResult parentResults = parentDrawResult;
                DrawMethodResult? siblingResults = siblingDrawResult;
                UISystem.RunAfterDrawingEntities(() =>
                {
                    _isCurrentlyDrawingOpenedListTopMost = true;
                    _DoDraw(parentResults, siblingResults);
                    _isCurrentlyDrawingOpenedListTopMost = false;
                });
            }
            // dropdown is closed - render normally in close state
            else
            {
                base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);
            }

            // opened? resize to close state AFTER rendering
            // this way we render the entire open list, but without pushing down auto anchors below (ie the entity only takes the size of its close state when positioning).
            if (IsOpened)
            {
                SetSizeToClosedState(ref boundingRect, ref internalBoundingRect);
            }
        }
        bool _isCurrentlyDrawingOpenedListTopMost = false;

        /// <summary>
        /// Close the dropdown list.
        /// </summary>
        public void CloseList()
        {
            IsOpened = false;
        }

        /// <summary>
        /// Open the dropdown list.
        /// </summary>
        public void OpenList()
        {
            IsOpened = true;
        }

        /// <summary>
        /// Toggle dropdown list state.
        /// </summary>
        public void ToggleList()
        {
            if (IsOpened)
            {
                CloseList();
            }
            else
            {
                OpenList();
            }
        }

        /// <inheritdoc/>
        protected override void OnItemClicked(Entity entity)
        {
            // if closed, open the list
            if (!IsOpened)
            {
                OpenList();
            }
            // if opened, call default action and close the list
            else
            {
                base.OnItemClicked(entity);
                CloseList();
            }
        }

        /// <inheritdoc/>
        protected override void SetParagraphs(int scrollOffset, int startIndex = 0)
        {
            if (IsOpened)
            {
                base.SetParagraphs(scrollOffset, startIndex);
            }
            else
            {
                foreach (var p in _paragraphs)
                {
                    p.Visible = false;
                }
            }
        }

        /// <inheritdoc/>
        internal override void PostUpdate(InputState inputState)
        {
            // update selected value text
            _selectedValueParagraph.Text = OverrideSelectedText ?? SelectedText ?? SelectedValue ?? DefaultSelectedText ?? string.Empty;
            _selectedValueParagraph.UseEmptyValueTextColor = (SelectedValue == null);

            // if opened and click outside, close the list
            if (IsOpened && inputState.LeftMousePressedNow)
            {
                // clicked on closed state box? skip
                if (_selectedValuePanel.IsPointedOn(inputState.MousePosition))
                {
                    return;
                }

                // if got here and point outside the list, close.
                if (!IsPointedOn(inputState.MousePosition))
                {
                    CloseList();
                }
            }
        }
    }
}
