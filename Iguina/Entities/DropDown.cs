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
        /// Create the drop down.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Drop down panel stylesheet.</param>
        /// <param name="itemsStylesheet">Drop down box items stylesheet. If not set, will use the same as base stylesheet.</param>
        public DropDown(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null) : base(system, stylesheet, itemsStylesheet)
        {
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
        /// Set bounding rectangles size to its closed state.
        /// </summary>
        private void SetSizeToClosedState(ref Rectangle boundingRect, ref Rectangle internalBoundingRect)
        {
            var padding = GetPadding();
            var extra = GetExtraSize();
            internalBoundingRect.Height = ItemHeight + padding.Top + padding.Bottom + extra.Top + extra.Bottom;
            boundingRect.Height = internalBoundingRect.Height;
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // special - if we are rendering in open mode, top most
            if (_isInTopmostDraw)
            {
                base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);
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
                DrawMethodResult parentResults = parentDrawResult;
                DrawMethodResult? siblingResults = siblingDrawResult;
                UISystem.RunAfterDrawingEntities(() =>
                {
                    _isInTopmostDraw = true;
                    _DoDraw(parentResults, siblingResults);
                    _isInTopmostDraw = false;
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
        bool _isInTopmostDraw = false;

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
            // if opened and clicked outside, close list
            if (IsOpened && !IsPointedOn(inputState.MousePosition) && inputState.LeftMousePressedNow)
            {
                IsOpened = false;
            }
        }

        /// <inheritdoc/>
        protected override void SetParagraphs(int scrollOffset)
        {
            // if opened, set list paragraphs as-is
            if (IsOpened)
            {
                if (_paragraphs.Count > 0) { _paragraphs[0].UseEmptyValueTextColor = false; }
                base.SetParagraphs(scrollOffset);
            }
            // if not opened, hide all paragraphs except top which is used for selected
            else
            {
                if (_paragraphs.Count > 0)
                {
                    {
                        foreach (var p in _paragraphs)
                        {
                            p.Visible = false;
                        }
                    }
                    {
                        var p = _paragraphs[0];
                        p.LockedState = null;
                        p.Visible = true;
                        p.Text = SelectedText ?? SelectedValue ?? DefaultSelectedText ?? string.Empty;
                        p.UseEmptyValueTextColor = SelectedValue == null;
                    }
                }
            }
        }
    }
}
