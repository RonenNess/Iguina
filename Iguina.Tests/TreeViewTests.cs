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

        void CreateTree(int visibleRows = 10)
        {
            _renderer = new ProportionalRenderer();
            _input = new ScriptedInputProvider();
            _system = new UISystem(_renderer, _input);
            _tree = _system.Root.AddChild(new TreeView(_system));
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
