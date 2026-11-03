using Iguina.Defs;


namespace Iguina.Entities
{
    /// <summary>
    /// A scrollable box that shows a tree of nodes, where branches can be expanded / collapsed and nodes can be selected.
    /// </summary>
    /// <remarks>
    /// Add top-level nodes with <see cref="AddNode(string, string?)"/> (or via <see cref="Root"/>), and child nodes with <see cref="TreeNode.AddChild(string, string?)"/>.
    /// Only rows that are currently visible are created as entities, so trees can be big.
    /// </remarks>
    public class TreeView : Panel
    {
        /// <summary>
        /// Hidden root node. Its children are the top-level nodes of the tree.
        /// </summary>
        public TreeNode Root { get; private set; }

        /// <summary>
        /// Indent, in pixels, for every tree level.
        /// </summary>
        public int Indent = 24;

        /// <summary>
        /// If true, will show expand / collapse buttons next to branches.
        /// If false, branches are expanded / collapsed by clicking on their labels.
        /// </summary>
        public bool ShowExpandCollapseButtons = true;

        /// <summary>
        /// If true, nodes with children can be selected. If false, only leaves can be selected, and clicking on a branch label toggles it.
        /// Can be overridden per node with <see cref="TreeNode.Selectable"/>.
        /// </summary>
        public bool BranchesSelectable = true;

        /// <summary>
        /// If true, clicking on a branch label will also expand / collapse it (in addition to selecting it, if selectable).
        /// Branches that can't be selected, or trees without expand / collapse buttons, always toggle on label click.
        /// </summary>
        public bool ToggleBranchesOnLabelClick = false;

        /// <summary>
        /// If true and user clicks on the selected node, it will deselect it.
        /// </summary>
        public bool AllowDeselect = false;

        /// <summary>
        /// Text to show on the button of a collapsed branch.
        /// </summary>
        public string ExpandSymbol = "+";

        /// <summary>
        /// Text to show on the button of an expanded branch.
        /// </summary>
        public string CollapseSymbol = "-";

        /// <summary>
        /// Text to show in the button column of leaves (empty by default).
        /// </summary>
        public string LeafSymbol = string.Empty;

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for nodes labels.
        /// </summary>
        public StyleSheetState OverrideItemStyles = new();

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the selected node label.
        /// </summary>
        public StyleSheetState OverrideSelectedItemStyles = new();

        /// <summary>
        /// Styles to override stylesheet defaults, regardless of entity state, for the expand / collapse buttons.
        /// </summary>
        public StyleSheetState OverrideToggleStyles = new();

        /// <summary>
        /// Stylesheet used for nodes labels.
        /// </summary>
        public StyleSheet ItemsStyleSheet { get; private set; }

        /// <summary>
        /// Stylesheet used for expand / collapse buttons.
        /// </summary>
        public StyleSheet TogglesStyleSheet { get; private set; }

        /// <summary>
        /// Called when a node is expanded or collapsed (by user or by code).
        /// </summary>
        public Action<TreeView, TreeNode>? OnNodeToggled;

        /// <summary>
        /// Called when a node label is clicked, even if the node is not selectable.
        /// </summary>
        public Action<TreeView, TreeNode>? OnNodeClicked;

        /// <summary>
        /// Get / set selected node.
        /// Set to null to clear selection. Setting a node that doesn't belong to this tree will throw exception.
        /// </summary>
        public TreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (value == _selectedNode) { return; }
                if ((value != null) && (value.Tree != this))
                {
                    throw new ArgumentException("Node to select does not belong to this tree view!");
                }
                _selectedNode = value;
                OnSelectedValueChanged();
            }
        }
        TreeNode? _selectedNode;

        /// <summary>
        /// Get / set selected node by value.
        /// Set to null to clear selection. Setting a value that doesn't exist in tree will throw exception.
        /// </summary>
        public string? SelectedValue
        {
            get => _selectedNode?.Value;
            set
            {
                if (value == null) { SelectedNode = null; return; }
                if (_selectedNode?.Value == value) { return; }
                SelectedNode = FindNode(value) ?? throw new KeyNotFoundException("Value to select was not found in tree!");
            }
        }

        /// <summary>
        /// The height, in pixels, of a single row, including its margins.
        /// </summary>
        public int RowHeight
        {
            get
            {
                UpdateRowMetrics();
                return _rowHeight;
            }
        }
        int _rowHeight = 1;

        /// <summary>
        /// How many rows (visible nodes) the tree currently has, as calculated on last draw.
        /// </summary>
        public int RowsCount => _rows.Count;

        /// <summary>
        /// How many rows fully fit in the view, as calculated on the last draw.
        /// </summary>
        public int VisibleRowsCount { get; private set; }

        /// <summary>
        /// Get / set the index of the first visible row (the scroll offset).
        /// When set, the value is clamped to the valid range on the next draw.
        /// </summary>
        public int ScrollOffset
        {
            get => _pendingScrollOffset ?? VerticalScrollbar?.Value ?? 0;
            set => _pendingScrollOffset = Math.Max(0, value);
        }
        int? _pendingScrollOffset;

        // currently shown nodes, in order
        readonly List<TreeNode> _rows = new();

        // entities to present rows
        class RowEntities
        {
            public Paragraph Toggle = null!;
            public Paragraph Label = null!;
        }
        readonly List<RowEntities> _rowEntities = new();

        // paragraphs used to resolve style and measure, without drawing them
        readonly Paragraph _itemsProbe;
        readonly Paragraph _togglesProbe;

        // row metrics
        int _lineHeight = 1;
        int _rowTopOffset;
        int _toggleColumnWidth;
        int _availableWidth;

        /// <inheritdoc/>
        internal override bool Interactable => true;

        /// <inheritdoc/>
        protected override bool WalkInternalChildren => true;

        /// <summary>
        /// Create the tree view.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        /// <param name="stylesheet">Tree view panel stylesheet.</param>
        /// <param name="itemsStylesheet">Nodes labels stylesheet. If not set, will use default list items or paragraphs stylesheet.</param>
        /// <param name="togglesStylesheet">Expand / collapse buttons stylesheet. If not set, will use items stylesheet.</param>
        public TreeView(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet = null, StyleSheet? togglesStylesheet = null) : base(system, stylesheet)
        {
            ItemsStyleSheet = itemsStylesheet ?? system.DefaultStylesheets.TreeViewItems ?? system.DefaultStylesheets.ListItems ?? system.DefaultStylesheets.Paragraphs ?? StyleSheet;
            TogglesStyleSheet = togglesStylesheet ?? system.DefaultStylesheets.TreeViewToggles ?? ItemsStyleSheet;

            Root = new TreeNode(string.Empty) { _ownerTree = this };
            Root.Expanded = true;

            // set defaults
            OverflowMode = OverflowMode.HideOverflow;

            // create probes for measuring
            _itemsProbe = new Paragraph(system, ItemsStyleSheet, string.Empty, false) { LockedState = EntityState.Default };
            _togglesProbe = new Paragraph(system, TogglesStyleSheet, string.Empty, false) { LockedState = EntityState.Default };
            UpdateRowMetrics();

            // create scrollbar (we set its max value ourselves)
            CreateVerticalScrollbar(false);
        }

        /// <summary>
        /// Create the tree view with default stylesheets.
        /// </summary>
        /// <param name="system">Parent UI system.</param>
        public TreeView(UISystem system) : this(system, system.DefaultStylesheets.TreeViewPanels ?? system.DefaultStylesheets.ListPanels ?? system.DefaultStylesheets.Panels)
        {
        }

        /// <summary>
        /// Add a top-level node.
        /// </summary>
        /// <param name="value">Node value.</param>
        /// <param name="label">Node label, or null to show value instead.</param>
        /// <returns>The new node.</returns>
        public TreeNode AddNode(string value, string? label = null)
        {
            return Root.AddChild(value, label);
        }

        /// <summary>
        /// Add a node as top-level node.
        /// </summary>
        /// <param name="node">Node to add.</param>
        /// <returns>The added node.</returns>
        public TreeNode AddNode(TreeNode node)
        {
            return Root.AddChild(node);
        }

        /// <summary>
        /// Remove all nodes from tree.
        /// </summary>
        public void Clear()
        {
            Root.ClearChildren();
        }

        /// <summary>
        /// Find the first node with a given value.
        /// </summary>
        /// <param name="value">Value to find.</param>
        /// <returns>Node, or null if not found.</returns>
        public TreeNode? FindNode(string value)
        {
            foreach (var node in Root.Traverse())
            {
                if ((node != Root) && (node.Value == value)) { return node; }
            }
            return null;
        }

        /// <summary>
        /// Expand all nodes.
        /// </summary>
        public void ExpandAll()
        {
            Root.ExpandAll();
        }

        /// <summary>
        /// Collapse all nodes.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var node in Root.Children) { node.CollapseAll(); }
        }

        /// <summary>
        /// Check if a node can be selected.
        /// </summary>
        /// <param name="node">Node to check.</param>
        /// <returns>True if node is selectable.</returns>
        public bool IsSelectable(TreeNode node)
        {
            return node.Selectable ?? (!node.HasChildren || BranchesSelectable);
        }

        /// <summary>
        /// Expand all parents of a node and scroll to make it visible.
        /// </summary>
        /// <param name="node">Node to show.</param>
        public void ScrollToNode(TreeNode node)
        {
            node.ExpandParents();
            UpdateRows();
            var index = _rows.IndexOf(node);
            if (index < 0) { return; }
            long offset = ScrollOffset;
            long visible = Math.Max(1, VisibleRowsCount);
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
        /// Scroll to the selected node, if there is one.
        /// </summary>
        public void ScrollToSelected()
        {
            if (_selectedNode != null) { ScrollToNode(_selectedNode); }
        }

        /// <summary>
        /// Set the height of this tree view to show exactly this number of rows.
        /// </summary>
        /// <param name="rows">How many rows should be visible.</param>
        public void SetVisibleRowsCount(int rows)
        {
            AutoHeight = false;
            Size.Y.SetPixels(RowHeight * Math.Max(rows, 1) + GetNonContentHeight());
        }

        /// <summary>
        /// Called whenever the selected node changes.
        /// By default, triggers the value changed events.
        /// </summary>
        protected virtual void OnSelectedValueChanged()
        {
            Events.OnValueChanged?.Invoke(this);
            UISystem.Events.OnValueChanged?.Invoke(this);
        }

        /// <summary>
        /// Called by nodes when expanded / collapsed.
        /// </summary>
        internal void NotifyNodeToggled(TreeNode node)
        {
            if (node != Root) { OnNodeToggled?.Invoke(this, node); }
        }

        /// <summary>
        /// Called by nodes when a node is removed from tree.
        /// </summary>
        internal void NotifyNodeRemoved(TreeNode node)
        {
            if ((_selectedNode != null) && ((_selectedNode == node) || _selectedNode.IsDescendantOf(node)))
            {
                SelectedNode = null;
            }
        }

        /// <summary>
        /// Toggle a node expanded state, if it has children.
        /// </summary>
        void ToggleNode(TreeNode node)
        {
            if (node.HasChildren)
            {
                node.Expanded = !node.Expanded;
            }
        }

        /// <summary>
        /// Handle click on node label.
        /// </summary>
        protected virtual void HandleLabelClick(TreeNode node)
        {
            OnNodeClicked?.Invoke(this, node);

            bool selectable = IsSelectable(node);
            if (selectable)
            {
                if (AllowDeselect && (node == _selectedNode)) { SelectedNode = null; }
                else { SelectedNode = node; }
            }

            if (node.HasChildren && (ToggleBranchesOnLabelClick || !ShowExpandCollapseButtons || !selectable))
            {
                ToggleNode(node);
            }
        }

        /// <summary>
        /// Rebuild the list of shown rows.
        /// </summary>
        void UpdateRows()
        {
            _rows.Clear();
            var stack = new Stack<TreeNode>();
            for (int i = Root.Children.Count - 1; i >= 0; --i) { stack.Push(Root.Children[i]); }
            while (stack.Count > 0)
            {
                var node = stack.Pop();
                _rows.Add(node);
                if (node.Expanded)
                {
                    for (int i = node.Children.Count - 1; i >= 0; --i) { stack.Push(node.Children[i]); }
                }
            }
        }

        /// <summary>
        /// Calculate row metrics from effective items style (stylesheet + overrides).
        /// </summary>
        void UpdateRowMetrics()
        {
            _itemsProbe.OverrideStyles = OverrideItemStyles;
            _togglesProbe.OverrideStyles = OverrideToggleStyles;
            _lineHeight = Math.Max(1, Math.Max(_itemsProbe.MeasureTextLineHeight(), ShowExpandCollapseButtons ? _togglesProbe.MeasureTextLineHeight() : 0));
            if (ItemsStyleSheet.DefaultHeight?.Units == MeasureUnit.Pixels)
            {
                _lineHeight = Math.Max(_lineHeight, (int)ItemsStyleSheet.DefaultHeight.Value.Value);
            }
            var marginBefore = _itemsProbe.GetMarginBefore();
            var marginAfter = _itemsProbe.GetMarginAfter();
            var extra = _itemsProbe.GetExtraSize();
            _rowTopOffset = marginBefore.Y + extra.Top;
            _rowHeight = Math.Max(1, _lineHeight + marginBefore.Y + marginAfter.Y + extra.Top + extra.Bottom);

            // toggle column width: widest symbol + a space
            if (ShowExpandCollapseButtons)
            {
                var widest = Math.Max(Math.Max(_togglesProbe.MeasureText(ExpandSymbol).X, _togglesProbe.MeasureText(CollapseSymbol).X), _togglesProbe.MeasureText(LeafSymbol).X);
                _toggleColumnWidth = widest + _togglesProbe.MeasureText(" ").X;
            }
            else
            {
                _toggleColumnWidth = 0;
            }
        }

        /// <summary>
        /// Get how much of the entity height is not used for rows (padding minus extra size).
        /// </summary>
        int GetNonContentHeight()
        {
            var padding = GetPadding();
            var extra = GetExtraSize();
            return Math.Max(0, padding.Top + padding.Bottom - extra.Top - extra.Bottom);
        }

        /// <summary>
        /// Get how much width the vertical scrollbar takes when visible.
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
            return Math.Max(0, width + scrollbar.GetMarginBefore().X);
        }

        /// <inheritdoc/>
        protected override Point GetScrollOffset()
        {
            // scrollbar changes which rows we show, so we don't need offset.
            return Point.Zero;
        }

        /// <inheritdoc/>
        protected override void SetAutoSizes(int maxWidth, int maxHeight)
        {
            if (AutoWidth)
            {
                if (AutoWidthMaxSize.HasValue && maxWidth > AutoWidthMaxSize.Value)
                {
                    maxWidth = AutoWidthMaxSize.Value;
                }
                Size.X.SetPixels(maxWidth);
            }

            if (AutoHeight)
            {
                UpdateRows();
                var height = (int)Math.Min((long)RowHeight * Math.Max(_rows.Count, 1) + GetNonContentHeight(), int.MaxValue);
                if (AutoHeightMaxSize.HasValue && height > AutoHeightMaxSize.Value)
                {
                    height = AutoHeightMaxSize.Value;
                }
                Size.Y.SetPixels(height);
            }
        }

        /// <summary>
        /// Create paragraph for a row part.
        /// </summary>
        Paragraph CreateRowParagraph(StyleSheet stylesheet, bool isToggle)
        {
            var p = new Paragraph(UISystem, stylesheet, string.Empty, false);
            p.TextOverflowMode = TextOverflowMode.Overflow;
            p.ShrinkToMinimalSize = false;
            p.PassFocusTo = this;
            p._overrideInteractableState = true;
            p.Anchor = Anchor.TopLeft;

            // pass events from paragraph to self
            p.Events.OnMouseWheelScrollUp = (Entity entity) => { this.Events.OnMouseWheelScrollUp?.Invoke(this); };
            p.Events.OnMouseWheelScrollDown = (Entity entity) => { this.Events.OnMouseWheelScrollDown?.Invoke(this); };
            p.Events.OnLeftMouseDown = (Entity entity) => { this.Events.OnLeftMouseDown?.Invoke(this); };
            p.Events.OnLeftMousePressed = (Entity entity) => { this.Events.OnLeftMousePressed?.Invoke(this); };
            p.Events.OnLeftMouseReleased = (Entity entity) => { this.Events.OnLeftMouseReleased?.Invoke(this); };
            p.Events.OnRightMouseDown = (Entity entity) => { this.Events.OnRightMouseDown?.Invoke(this); };
            p.Events.OnRightMousePressed = (Entity entity) => { this.Events.OnRightMousePressed?.Invoke(this); };
            p.Events.OnRightMouseReleased = (Entity entity) => { this.Events.OnRightMouseReleased?.Invoke(this); };
            p.Events.WhileMouseHover = (Entity entity) => { this.Events.WhileMouseHover?.Invoke(this); };

            // click: toggle or select node
            p.Events.OnClick = (Entity entity) =>
            {
                if (entity.UserData is TreeNode node)
                {
                    if (isToggle) { ToggleNode(node); }
                    else { HandleLabelClick(node); }
                }
                this.Events.OnClick?.Invoke(this);
            };

            AddChildInternal(p);
            p.IgnoreScrollOffset = true;
            return p;
        }

        /// <inheritdoc/>
        protected override void DrawEntityType(ref Rectangle boundingRect, ref Rectangle internalBoundingRect, DrawMethodResult parentDrawResult, DrawMethodResult? siblingDrawResult)
        {
            // render base
            base.DrawEntityType(ref boundingRect, ref internalBoundingRect, parentDrawResult, siblingDrawResult);

            // update metrics and rows
            UpdateRowMetrics();
            UpdateRows();

            // calculate visible rows and scroll range
            int viewHeight = Math.Max(0, internalBoundingRect.Height);
            VisibleRowsCount = Math.Max(1, viewHeight / _rowHeight);
            int maxOffset = Math.Max(0, _rows.Count - VisibleRowsCount);
            bool showScrollbar = (maxOffset > 0) && (VerticalScrollbar != null);
            _availableWidth = internalBoundingRect.Width - (showScrollbar ? GetScrollbarReservedWidth(internalBoundingRect) : 0);

            // update scrollbar and get scroll offset
            int scrollOffset = 0;
            if (VerticalScrollbar != null)
            {
                VerticalScrollbar.MaxValue = maxOffset;
                VerticalScrollbar.Visible = showScrollbar;
                if (_pendingScrollOffset.HasValue)
                {
                    VerticalScrollbar.ValueSafe = _pendingScrollOffset.Value;
                    _pendingScrollOffset = null;
                }
                scrollOffset = Math.Clamp(VerticalScrollbar.Value, 0, maxOffset);
            }

            // how many rows to present (including partially visible last row)
            int rowsToShow = Math.Min((int)Math.Ceiling((float)viewHeight / (float)_rowHeight), _rows.Count - scrollOffset);
            rowsToShow = Math.Max(0, rowsToShow);

            // create / remove row entities
            while (_rowEntities.Count < rowsToShow)
            {
                _rowEntities.Add(new RowEntities()
                {
                    Toggle = CreateRowParagraph(TogglesStyleSheet, true),
                    Label = CreateRowParagraph(ItemsStyleSheet, false)
                });
            }
            while (_rowEntities.Count > rowsToShow)
            {
                var last = _rowEntities[^1];
                RemoveChildInternal(last.Toggle);
                RemoveChildInternal(last.Label);
                _rowEntities.RemoveAt(_rowEntities.Count - 1);
            }

            // set rows
            // note: paragraphs shrink parent rect by 12 pixels (see Paragraph.ProcessParentInternalRect)
            int rowsWidth = Math.Max(1, _availableWidth - 12);
            for (int i = 0; i < rowsToShow; ++i)
            {
                var node = _rows[scrollOffset + i];
                var entities = _rowEntities[i];
                int y = i * _rowHeight + _rowTopOffset;
                int x = Math.Max(0, node.Depth) * Indent;

                // toggle button
                var toggle = entities.Toggle;
                toggle.Visible = ShowExpandCollapseButtons;
                toggle.UserData = node;
                toggle.OverrideStyles = OverrideToggleStyles;
                toggle.Text = node.HasChildren ? (node.Expanded ? CollapseSymbol : ExpandSymbol) : LeafSymbol;
                toggle.IgnoreInteractions = !node.HasChildren;
                toggle.Size.X.SetPixels(Math.Max(1, _toggleColumnWidth));
                toggle.Size.Y.SetPixels(_lineHeight);
                toggle.Offset.X.SetPixels(x);
                toggle.Offset.Y.SetPixels(y);

                // label
                var label = entities.Label;
                bool selected = (node == _selectedNode);
                label.Visible = true;
                label.UserData = node;
                label.Text = node.DisplayText;
                label.LockedState = selected ? EntityState.Checked : null;
                label.OverrideStyles = selected ? OverrideSelectedItemStyles : OverrideItemStyles;
                int labelX = x + _toggleColumnWidth;
                label.Size.X.SetPixels(Math.Max(1, rowsWidth - labelX));
                label.Size.Y.SetPixels(_lineHeight);
                label.Offset.X.SetPixels(labelX);
                label.Offset.Y.SetPixels(y);
            }
        }

        /// <summary>
        /// Select next / previous selectable row.
        /// </summary>
        void MoveSelection(int direction)
        {
            UpdateRows();
            if (_rows.Count == 0) { return; }
            int index = (_selectedNode != null) ? _rows.IndexOf(_selectedNode) : -1;
            if (index < 0) { index = (direction > 0) ? -1 : _rows.Count; }
            for (int i = index + direction; i >= 0 && i < _rows.Count; i += direction)
            {
                if (IsSelectable(_rows[i]))
                {
                    SelectedNode = _rows[i];
                    ScrollToNode(_rows[i]);
                    return;
                }
            }
        }

        /// <inheritdoc/>
        internal override void DoFocusedEntityInteractions(InputState inputState)
        {
            // call base class to trigger events
            base.DoFocusedEntityInteractions(inputState);

            switch (inputState.KeyboardInteraction)
            {
                // move selection up / down
                case Drivers.KeyboardInteractions.MoveUp:
                    MoveSelection(-1);
                    break;

                case Drivers.KeyboardInteractions.MoveDown:
                    MoveSelection(1);
                    break;

                // collapse selected node, or select its parent
                case Drivers.KeyboardInteractions.MoveLeft:
                    if (_selectedNode != null)
                    {
                        if (_selectedNode.HasChildren && _selectedNode.Expanded)
                        {
                            _selectedNode.Expanded = false;
                        }
                        else if ((_selectedNode.Parent != null) && (_selectedNode.Parent != Root) && IsSelectable(_selectedNode.Parent))
                        {
                            SelectedNode = _selectedNode.Parent;
                            ScrollToNode(_selectedNode);
                        }
                    }
                    break;

                // expand selected node
                case Drivers.KeyboardInteractions.MoveRight:
                    if ((_selectedNode != null) && _selectedNode.HasChildren)
                    {
                        _selectedNode.Expanded = true;
                    }
                    break;
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
