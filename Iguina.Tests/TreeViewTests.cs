using Iguina.Defs;
using Iguina.Drivers;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests for the tree view entity.
    /// </summary>
    public class TreeViewTests
    {
        ProportionalRenderer _renderer = null!;
        ScriptedInputProvider _input = null!;
        UISystem _system = null!;
        TreeView _tree = null!;
        List<string> _toggled = new();
        int _valueChangedCount;

        void CreateTree(int visibleRows = 10, StyleSheet? togglesStyle = null, string? themeFolder = null, StyleSheet? itemsStyle = null)
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = (themeFolder != null) ? new UISystem(Path.Combine(themeFolder, "system_style.json"), _renderer, _input) : new UISystem(_renderer, _input);
            _tree = _system.Root.AddChild(((togglesStyle != null) || (itemsStyle != null)) ? new TreeView(_system, _system.DefaultStylesheets.Panels, itemsStyle, togglesStyle) : new TreeView(_system));
            _tree.Anchor = Anchor.TopLeft;
            _tree.Size.X.SetPixels(400);
            _tree.SetVisibleRowsCount(visibleRows);
            _tree.OnNodeToggled = (TreeView tree, TreeNode node) => _toggled.Add(node.Value + (node.Expanded ? "+" : "-"));
            _tree.Events.OnValueChanged = (Entity entity) => _valueChangedCount++;
            _toggled.Clear();
            _valueChangedCount = 0;
            _input.MousePosition = new Point(2000, 2000);

            // A (expanded) -> A1, A2 -> A2a ; B
            var a = _tree.AddNode("a", "A");
            a.AddChild("a1", "A1");
            var a2 = a.AddChild("a2", "A2");
            a2.AddChild("a2a", "A2a");
            _tree.AddNode("b", "B");
            a.Expanded = true;
            _toggled.Clear();
            Frames(2);
        }

        void Frames(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                _renderer.ClearRecords();
                _system.Update(0.01f);
                _system.Draw();
            }
        }

        void ClickAt(Point position)
        {
            _input.MousePosition = position;
            Frames(1);
            _input.LeftDown = true;
            Frames(1);
            _input.LeftDown = false;
            Frames(1);
        }

        Point TextPosition(string text)
        {
            var found = _renderer.DrawnTexts.Where(x => x.Text == text).Distinct().ToList();
            Assert.That(found, Has.Count.EqualTo(1), $"Text '{text}' drawn count");
            return found[0].Position;
        }

        void ClickText(string text)
        {
            var pos = TextPosition(text);
            ClickAt(new Point(pos.X + 2, pos.Y + 5));
        }

        // note: distinct since texts in transition between states are drawn twice
        List<string> DrawnLabels() => _renderer.DrawnTexts.Where(x => x.Text != "+" && x.Text != "-").Distinct().OrderBy(x => x.Position.Y).Select(x => x.Text).ToList();

        void PressKey(KeyboardInteractions key)
        {
            _input.KeyboardInteraction = key;
            Frames(2);
        }

        [Test]
        public void TestRowsIndentAndSpacing()
        {
            CreateTree();
            Assert.That(DrawnLabels(), Is.EqualTo(new[] { "A", "A1", "A2", "B" }));
            Assert.That(TextPosition("A1").X - TextPosition("A").X, Is.EqualTo(_tree.Indent));
            Assert.That(TextPosition("A1").Y - TextPosition("A").Y, Is.EqualTo(_tree.RowHeight));
            Assert.That(TextPosition("B").X, Is.EqualTo(TextPosition("A").X));
            Assert.That(_renderer.DrawnTexts.Distinct().Count(x => x.Text == "-"), Is.EqualTo(1));
            Assert.That(_renderer.DrawnTexts.Distinct().Count(x => x.Text == "+"), Is.EqualTo(1));
            Assert.That(_tree.RowsCount, Is.EqualTo(4));
            Assert.That(_tree.VerticalScrollbar!.Visible, Is.False);
        }

        [Test]
        public void TestExpandCollapseByButton()
        {
            CreateTree();
            ClickText("+");
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);
            Assert.That(_toggled, Is.EqualTo(new[] { "a2+" }));
            Assert.That(DrawnLabels(), Is.EqualTo(new[] { "A", "A1", "A2", "A2a", "B" }));
            Assert.That(_tree.SelectedNode, Is.Null);

            // collapse top node hides all its descendants
            var collapseA = _renderer.DrawnTexts.Where(x => x.Text == "-").OrderBy(x => x.Position.Y).First().Position;
            ClickAt(new Point(collapseA.X + 2, collapseA.Y + 5));
            Assert.That(DrawnLabels(), Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void TestSelection()
        {
            CreateTree();
            ClickText("A1");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a1"));
            Assert.That(_valueChangedCount, Is.EqualTo(1));

            // branch selectable by default, without toggling
            ClickText("A");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a"));
            Assert.That(_tree.FindNode("a")!.Expanded, Is.True);

            // branches not selectable: clicking toggles instead
            _tree.BranchesSelectable = false;
            ClickText("A2");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a"));
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);
            Assert.That(_tree.IsSelectable(_tree.FindNode("a2")!), Is.False);

            // per node override
            _tree.FindNode("a2")!.Selectable = true;
            ClickText("A2");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a2"));

            // deselect
            _tree.AllowDeselect = true;
            ClickText("A2");
            Assert.That(_tree.SelectedNode, Is.Null);

            // by code
            _tree.SelectedValue = "a2a";
            Assert.That(_tree.SelectedNode, Is.SameAs(_tree.FindNode("a2a")));
            Assert.Throws<KeyNotFoundException>(() => _tree.SelectedValue = "nope");
            Assert.Throws<ArgumentException>(() => _tree.SelectedNode = new TreeNode("x"));

            // removing selected node (or its ancestor) clears selection
            _tree.FindNode("a2")!.Remove();
            Assert.That(_tree.SelectedNode, Is.Null);
            Frames(1);
            Assert.That(DrawnLabels(), Is.EqualTo(new[] { "A", "A1", "B" }));
        }

        [Test]
        public void TestWithoutExpandCollapseButtons()
        {
            CreateTree();
            _tree.ShowExpandCollapseButtons = false;
            Frames(1);
            Assert.That(_renderer.DrawnTexts.Any(x => x.Text == "+" || x.Text == "-"), Is.False);
            Assert.That(TextPosition("A1").X - TextPosition("A").X, Is.EqualTo(_tree.Indent));

            // clicking a branch label selects and toggles it
            ClickText("A2");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a2"));
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);
        }

        [Test]
        public void TestScrollingAndScrollToNode()
        {
            CreateTree(5);
            var big = _tree.AddNode("big", "Big");
            for (int i = 0; i < 100; ++i)
            {
                var child = big.AddChild("c" + i, "Child " + i);
                child.AddChild("d" + i, "Deep " + i);
            }
            Frames(2);
            Assert.That(_tree.VerticalScrollbar!.Visible, Is.False);

            var target = _tree.FindNode("d50")!;
            _tree.ScrollToNode(target);
            Frames(2);
            Assert.That(big.Expanded && target.Parent!.Expanded, Is.True);
            Assert.That(_tree.VerticalScrollbar.Visible, Is.True);
            Assert.That(_tree.VisibleRowsCount, Is.EqualTo(5));
            Assert.That(DrawnLabels(), Does.Contain("Deep 50"));
            Assert.That(DrawnLabels().Count, Is.LessThanOrEqualTo(6));

            // labels don't go under the scrollbar
            var scrollbarLeft = _tree.VerticalScrollbar.LastBoundingRect.Left;
            foreach (var text in _renderer.DrawnTexts)
            {
                Assert.That(text.Position.X + ProportionalRenderer.Measure(text.Text), Is.LessThanOrEqualTo(scrollbarLeft));
            }

            _tree.CollapseAll();
            Frames(2);
            Assert.That(_tree.ScrollOffset, Is.EqualTo(0));
            Assert.That(DrawnLabels(), Is.EqualTo(new[] { "A", "B", "Big" }));
        }

        [Test]
        public void TestKeyboard()
        {
            CreateTree();
            ClickText("A");
            Assert.That(_tree.SelectedValue, Is.EqualTo("a"));

            PressKey(KeyboardInteractions.MoveDown);
            Assert.That(_tree.SelectedValue, Is.EqualTo("a1"));

            PressKey(KeyboardInteractions.MoveDown);
            Assert.That(_tree.SelectedValue, Is.EqualTo("a2"));

            PressKey(KeyboardInteractions.MoveRight);
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);

            PressKey(KeyboardInteractions.MoveLeft);
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.False);

            PressKey(KeyboardInteractions.MoveLeft);
            Assert.That(_tree.SelectedValue, Is.EqualTo("a"));

            // skip non-selectable branches
            _tree.BranchesSelectable = false;
            _tree.SelectedValue = "a1";
            PressKey(KeyboardInteractions.MoveDown);
            Assert.That(_tree.SelectedValue, Is.EqualTo("b"));
        }

        static readonly Rectangle ExpandIconSrc = new Rectangle(0, 0, 10, 10);
        static readonly Rectangle CollapseIconSrc = new Rectangle(10, 0, 10, 12);
        static readonly Rectangle LeafIconSrc = new Rectangle(20, 0, 8, 10);

        static StyleSheet CreateTogglesStyle(bool withTexts, bool withIcons)
        {
            var state = new StyleSheetState();
            if (withTexts)
            {
                state.TreeExpandText = "[+]";
                state.TreeCollapseText = "[-]";
                state.TreeLeafText = "*";
            }
            if (withIcons)
            {
                state.TreeExpandIcon = new IconTexture() { SourceRect = ExpandIconSrc };
                state.TreeCollapseIcon = new IconTexture() { SourceRect = CollapseIconSrc };
                state.TreeLeafIcon = new IconTexture() { SourceRect = LeafIconSrc };
            }
            return new StyleSheet() { Default = state };
        }

        List<Rectangle> DrawnIcons(Rectangle source) => _renderer.DrawnTextures.Where(x => x.Source.Equals(source)).Select(x => x.Dest).Distinct().OrderBy(x => x.Y).ToList();

        [Test]
        public void TestToggleTextsFromStylesheet()
        {
            CreateTree(togglesStyle: CreateTogglesStyle(true, false));
            Assert.That(_tree.EffectiveExpandSymbol, Is.EqualTo("[+]"));
            Assert.That(TextPosition("[+]").Y, Is.EqualTo(TextPosition("A2").Y));
            Assert.That(TextPosition("[-]").Y, Is.EqualTo(TextPosition("A").Y));
            Assert.That(_renderer.DrawnTexts.Where(x => x.Text == "*").Select(x => x.Position).Distinct().Count(), Is.EqualTo(2));
            Assert.That(TextPosition("A1").X - TextPosition("A").X, Is.EqualTo(_tree.Indent));

            // labels are placed after the widest toggle text
            Assert.That(TextPosition("A").X, Is.GreaterThanOrEqualTo(TextPosition("[-]").X + ProportionalRenderer.Measure("[-]")));

            // instance values override stylesheet, and null goes back to stylesheet
            _tree.ExpandSymbol = ">";
            Frames(1);
            Assert.That(TextPosition(">").Y, Is.EqualTo(TextPosition("A2").Y));
            Assert.That(_renderer.DrawnTexts.Any(x => x.Text == "[+]"), Is.False);
            _tree.ExpandSymbol = null;
            Frames(1);
            Assert.That(TextPosition("[+]").Y, Is.EqualTo(TextPosition("A2").Y));

            // override styles per instance
            _tree.OverrideToggleStyles.TreeCollapseText = "v";
            Frames(1);
            Assert.That(TextPosition("v").Y, Is.EqualTo(TextPosition("A").Y));

            // still clickable with custom texts
            ClickText("[+]");
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);
        }

        [Test]
        public void TestDefaultToggleTextsWithoutStylesheet()
        {
            CreateTree();
            Assert.That(_tree.EffectiveExpandSymbol, Is.EqualTo("+"));
            Assert.That(_tree.EffectiveCollapseSymbol, Is.EqualTo("-"));
            Assert.That(_tree.EffectiveLeafSymbol, Is.EqualTo(string.Empty));
            Assert.That(_renderer.DrawnTextures, Is.Empty);
        }

        [Test]
        public void TestIconsFromStylesheet()
        {
            CreateTree(togglesStyle: CreateTogglesStyle(false, true));

            // rows: A (expanded), A1 (leaf), A2 (collapsed), B (leaf)
            Assert.That(DrawnIcons(CollapseIconSrc), Has.Count.EqualTo(1));
            Assert.That(DrawnIcons(ExpandIconSrc), Has.Count.EqualTo(1));
            Assert.That(DrawnIcons(LeafIconSrc), Has.Count.EqualTo(2));

            // icon is between the toggle text and the label, on the same row
            var collapseIcon = DrawnIcons(CollapseIconSrc)[0];
            var minus = TextPosition("-");
            var labelA = TextPosition("A");
            Assert.That(collapseIcon.X, Is.GreaterThanOrEqualTo(minus.X + ProportionalRenderer.Measure("-")));
            Assert.That(labelA.X, Is.GreaterThanOrEqualTo(collapseIcon.Right));
            Assert.That(collapseIcon.Y, Is.InRange(labelA.Y - 2, labelA.Y + 20));

            // all labels of the same depth are aligned, whatever icon they have
            Assert.That(TextPosition("B").X, Is.EqualTo(labelA.X));
            Assert.That(TextPosition("A1").X, Is.EqualTo(TextPosition("A2").X));

            // icon changes when branch is expanded
            ClickText("+");
            Assert.That(_tree.FindNode("a2")!.Expanded, Is.True);
            Assert.That(DrawnIcons(CollapseIconSrc), Has.Count.EqualTo(2));
            Assert.That(DrawnIcons(ExpandIconSrc), Is.Empty);

            // clicking icon selects node
            var leafIcon = DrawnIcons(LeafIconSrc)[0];
            ClickAt(new Point(leafIcon.X + 2, leafIcon.Y + 2));
            Assert.That(_tree.SelectedValue, Is.EqualTo("a1"));

            // per instance icon override and hiding icons
            _tree.OverrideToggleStyles.TreeLeafIcon = new IconTexture() { SourceRect = new Rectangle(50, 50, 10, 10) };
            Frames(1);
            Assert.That(DrawnIcons(LeafIconSrc), Is.Empty);
            Assert.That(DrawnIcons(new Rectangle(50, 50, 10, 10)), Is.Not.Empty);

            var labelXWithIcons = TextPosition("A").X;
            _tree.ShowIcons = false;
            Frames(1);
            Assert.That(_renderer.DrawnTextures, Is.Empty);
            Assert.That(TextPosition("A").X, Is.LessThan(labelXWithIcons));
        }

        [Test]
        public void TestIconsWithoutToggleButtons()
        {
            CreateTree(togglesStyle: CreateTogglesStyle(true, true));
            _tree.ShowExpandCollapseButtons = false;
            Frames(1);
            Assert.That(_renderer.DrawnTexts.Any(x => x.Text.StartsWith("[")), Is.False);
            Assert.That(DrawnIcons(CollapseIconSrc), Has.Count.EqualTo(1));
            Assert.That(TextPosition("A").X, Is.GreaterThanOrEqualTo(DrawnIcons(CollapseIconSrc)[0].Right));
        }

        [Test]
        public void TestSelectionHighlightDoesntCoverIcon()
        {
            // selected label highlight texture extends 20 pixels to the left of the label
            var highlightSrc = new Rectangle(100, 100, 4, 4);
            var itemsStyle = new StyleSheet()
            {
                Checked = new StyleSheetState()
                {
                    FillTextureStretched = new StretchedTexture() { SourceRect = highlightSrc, ExtraSize = new Sides(20, 10, 0, 0) }
                }
            };
            CreateTree(togglesStyle: CreateTogglesStyle(false, true), itemsStyle: itemsStyle);

            foreach (var value in new[] { "a", "a1", "b" })
            {
                _tree.SelectedValue = value;
                Frames(2);
                var highlight = _renderer.DrawnTextures.Where(x => x.Source.Equals(highlightSrc)).Select(x => x.Dest).Distinct().ToList();
                Assert.That(highlight, Has.Count.EqualTo(1));

                // find the icon on the same row
                var icon = _renderer.DrawnTextures.Where(x => !x.Source.Equals(highlightSrc) && Math.Abs(x.Dest.Center.Y - highlight[0].Center.Y) < 10).Select(x => x.Dest).Distinct().Single();
                Assert.That(highlight[0].X, Is.GreaterThanOrEqualTo(icon.Right), value);
            }
        }

        [Test]
        public void TestIconsToggleBranches()
        {
            CreateTree(togglesStyle: CreateTogglesStyle(false, true));
            _tree.IconsToggleBranches = true;
            Frames(1);

            // click on expanded branch icon collapses it without selecting
            var collapseIcon = DrawnIcons(CollapseIconSrc)[0];
            ClickAt(new Point(collapseIcon.X + 2, collapseIcon.Y + 2));
            Assert.That(_tree.FindNode("a")!.Expanded, Is.False);
            Assert.That(_tree.SelectedNode, Is.Null);
            Assert.That(_toggled, Is.EqualTo(new[] { "a-" }));

            // click again expands
            var expandIcon = DrawnIcons(ExpandIconSrc)[0];
            ClickAt(new Point(expandIcon.X + 2, expandIcon.Y + 2));
            Assert.That(_tree.FindNode("a")!.Expanded, Is.True);

            // leaf icon still selects
            var leafIcon = DrawnIcons(LeafIconSrc)[0];
            ClickAt(new Point(leafIcon.X + 2, leafIcon.Y + 2));
            Assert.That(_tree.SelectedValue, Is.EqualTo("a1"));

            // without the flag, branch icon selects
            _tree.IconsToggleBranches = false;
            collapseIcon = DrawnIcons(CollapseIconSrc)[0];
            ClickAt(new Point(collapseIcon.X + 2, collapseIcon.Y + 2));
            Assert.That(_tree.SelectedValue, Is.EqualTo("a"));
            Assert.That(_tree.FindNode("a")!.Expanded, Is.True);
        }

        [Test]
        public void TestDefaultThemeTreeStyle()
        {
            var themePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../Iguina.Demo/Assets/DefaultTheme"));
            CreateTree(themeFolder: themePath);
            Assert.That(_system.DefaultStylesheets.TreeViewToggles, Is.Not.Null);
            Assert.That(_tree.EffectiveExpandSymbol, Is.EqualTo("+"));
            Assert.That(_tree.EffectiveCollapseSymbol, Is.EqualTo("-"));

            // folder icons for branches, file icons for leaves
            var folder = new Rectangle(240, 64, 16, 16);
            var file = new Rectangle(240, 48, 16, 16);
            Assert.That(DrawnIcons(folder), Has.Count.EqualTo(2));
            Assert.That(DrawnIcons(file), Has.Count.EqualTo(2));
            Assert.That(_tree.RowHeight, Is.GreaterThanOrEqualTo(DrawnIcons(folder)[0].Height));

            // selected highlight doesn't cover the icon
            Assert.That(_system.DefaultStylesheets.TreeViewItems, Is.Not.Null);
            _tree.SelectedValue = "a1";
            Frames(2);
            var highlight = _renderer.DrawnTextures.Where(x => x.Source.Equals(new Rectangle(0, 80, 16, 16))).Select(x => x.Dest).Distinct().Single();
            var leafIcon = DrawnIcons(file).First(x => Math.Abs(x.Center.Y - highlight.Center.Y) < 15);
            Assert.That(highlight.X, Is.GreaterThanOrEqualTo(leafIcon.Right));
        }

        [Test]
        public void TestNodeStructure()
        {
            var root = new TreeNode("r");
            var child = root.AddChild("c");
            var grandChild = child.AddChild("g");
            Assert.That(grandChild.Depth, Is.EqualTo(2));
            Assert.That(root.Find("g"), Is.SameAs(grandChild));
            Assert.That(root.Traverse().Select(x => x.Value), Is.EqualTo(new[] { "r", "c", "g" }));
            Assert.Throws<InvalidOperationException>(() => grandChild.AddChild(root));

            // move node between parents
            var other = root.AddChild("o");
            other.AddChild(grandChild);
            Assert.That(child.Children, Is.Empty);
            Assert.That(grandChild.Parent, Is.SameAs(other));
            Assert.That(grandChild.Tree, Is.Null);
        }
    }
}
