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
        public StyleSheetState OverrideClosedStateTextStyles = new();

        /// <summary>
        /// If true, will always show the box with currently selected value / label, even when list is opened.
        /// If false, will hide the selected value box when list is shown.
        /// </summary>
        public bool ShowSelectedValueBoxWhenOpened = true;

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
                if (ShowSelectedValueBoxWhenOpened)
                {
                    Size.Y.SetPixels((int)(ItemHeight * (ItemsCount + 2f)));
                }
                else
                {
                    Size.Y.SetPixels(ItemHeight * (ItemsCount + 1));
                }
            }
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
        protected override int GetExtraParagraphsCount()
        {
            return ShowSelectedValueBoxWhenOpened ? -1 : 0;
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // special - if we are rendering in open mode, top most
            if (_isCurrentlyDrawingOpenedListTopMost)
            {
                if (ShowSelectedValueBoxWhenOpened)
                {
                    var height = -ItemHeight * 2;
                    internalBoundingRect.Height += height;
                    boundingRect.Height += height;
                }
                
                base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);
                
                if (ShowSelectedValueBoxWhenOpened)
                {
                    Rectangle rect = boundingRect;
                    rect.Height = GetClosedStateHeight();
                    DrawFillTextures(rect);
                }
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
                if (ShowSelectedValueBoxWhenOpened)
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

        /// <inheritdoc/>
        protected override void OnItemClicked(Entity entity)
        {
            // if closed, open the list
            if (!IsOpened)
            {
                IsOpened = true;
            }
            // if opened, call default action and close the list
            else
            {
                base.OnItemClicked(entity);
                IsOpened = false;
            }
        }

        /// <inheritdoc/>
        internal override void PostUpdate(InputState inputState)
        {
            // if opened and click outside, close the list
            if (IsOpened && inputState.LeftMousePressedNow)
            {
                // clicked on closed state box? skip
                if ((_paragraphs.Count > 0) && _paragraphs[0].IsPointedOn(inputState.MousePosition))
                {
                    return;
                }

                // if we show closed state box while opened, and clicked on it, skip
                if (ShowSelectedValueBoxWhenOpened)
                {
                    var rect = LastBoundingRect;
                    rect.Y -= GetClosedStateHeight();
                    if (rect.Contains(inputState.MousePosition))
                    {
                        return;
                    }
                }

                // if got here and point outside the list, close.
                if (!IsPointedOn(inputState.MousePosition))
                {
                    IsOpened = false;
                }
            }
        }

        /// <inheritdoc/>
        protected override void SetParagraphs(int scrollOffset, int startIndex = 0)
        {
            // if opened, set list paragraphs as-is
            if (IsOpened)
            {
                if (ShowSelectedValueBoxWhenOpened)
                {
                    SetFirstParagraphToSelected();
                    base.SetParagraphs(scrollOffset, 1);
                    if (_paragraphs.Count > 0)
                    {
                        _paragraphs[0].Text += '\n';
                    }
                }
                else
                {
                    // "fix" first paragraph back to normal state
                    if (_paragraphs.Count > 0)
                    {
                        var p = _paragraphs[0];
                        p.UseEmptyValueTextColor = false;
                        p.ExtraMarginForInteractions.TurnToZero();
                    }
                    base.SetParagraphs(scrollOffset);
                }
            }
            // if not opened, hide all paragraphs except top which is used for selected
            else
            {
                if (_paragraphs.Count > 0)
                {  
                    foreach (var p in _paragraphs)
                    {
                        p.Visible = false;
                    }      
                    SetFirstParagraphToSelected();
                }
            }
        }

        /// <summary>
        /// Set the first paragraph to selected state.
        /// </summary>
        private void SetFirstParagraphToSelected()
        {
            var p = _paragraphs[0];
            p.LockedState = null;
            p.Visible = true;
            var padding = GetPadding();
            p.ExtraMarginForInteractions.Set(padding.Left, padding.Right, padding.Top, padding.Bottom);
            p.OverrideStyles = OverrideClosedStateTextStyles;
            p.Text = OverrideSelectedText ?? SelectedText ?? SelectedValue ?? DefaultSelectedText ?? string.Empty;
            p.UseEmptyValueTextColor = (SelectedValue == null);
            p.UserData = null;
        }
    }
}
