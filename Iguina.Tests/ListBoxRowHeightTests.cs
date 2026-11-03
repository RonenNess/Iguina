using Iguina.Defs;
using Iguina.Entities;

namespace Iguina.Tests
{
    /// <summary>
    /// Tests that list box items are drawn exactly ItemHeight apart, matching layout math.
    /// </summary>
    public class ListBoxRowHeightTests
    {
        class TestListBox : ListBox
        {
            public TestListBox(UISystem system, StyleSheet? stylesheet, StyleSheet? itemsStylesheet) : base(system, stylesheet, itemsStylesheet) { }
            public TestListBox(UISystem system) : base(system) { }
            public int PublicItemHeight => ItemHeight;
        }

        ProportionalRenderer _renderer = null!;
        UISystem _system = null!;

        void Frames(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                _renderer.ClearRecords();
                _system.Update(0.01f);
                _system.Draw();
            }
        }

        static TestListBox AddItems(TestListBox list, int count)
        {
            list.Anchor = Anchor.TopLeft;
            list.Size.X.SetPixels(400);
            for (int i = 0; i < count; ++i) { list.AddItem("v" + i, "item " + i); }
            return list;
        }

        List<Point> DrawnItems() => _renderer.DrawnTexts.Where(x => x.Text.StartsWith("item ")).Select(x => x.Position).Distinct().OrderBy(x => x.Y).ToList();

        void AssertRowsSpacing(int expected)
        {
            var drawn = DrawnItems();
            Assert.That(drawn.Count, Is.GreaterThan(2));
            for (int i = 1; i < drawn.Count; ++i)
            {
                Assert.That(drawn[i].Y - drawn[i - 1].Y, Is.EqualTo(expected));
            }
        }

        [Test]
        public void TestItemsDrawnItemHeightApart()
        {
            _renderer = new ProportionalRenderer();
            _system = new UISystem(_renderer, new ScriptedInputProvider());
            var itemsStyle = new StyleSheet()
            {
                Default = new StyleSheetState()
                {
                    MarginBefore = new Point(0, 2),
                    MarginAfter = new Point(0, 6),
                    ExtraSize = new Sides(0, 0, 3, 5)
                }
            };
            var panelStyle = new StyleSheet() { Default = new StyleSheetState() { Padding = new Sides(10, 10, 12, 4) } };
            var list = AddItems(_system.Root.AddChild(new TestListBox(_system, panelStyle, itemsStyle)), 50);
            list.SetVisibleItemsCount(5);
            Frames(3);

            // line height (20) + margins + extra size
            Assert.That(list.PublicItemHeight, Is.EqualTo(20 + 2 + 6 + 3 + 5));
            AssertRowsSpacing(list.PublicItemHeight);

            // exactly 5 items fit in the content area, and the first item is not pushed out of it
            Assert.That(list.LastInternalBoundingRect.Height, Is.EqualTo(list.PublicItemHeight * 5));
            var first = DrawnItems()[0];
            Assert.That(first.Y, Is.GreaterThanOrEqualTo(list.LastInternalBoundingRect.Top));

            // overrides at runtime
            list.OverrideItemStyles.MarginBefore = new Point(0, 0);
            list.OverrideItemStyles.MarginAfter = new Point(0, 0);
            list.OverrideItemStyles.ExtraSize = new Sides(0, 0, 0, 0);
            Assert.That(list.PublicItemHeight, Is.EqualTo(20));
            Frames(2);
            AssertRowsSpacing(20);
        }

        [Test]
        public void TestDefaultThemeItemsSpacingUnchanged()
        {
            // the default theme list items have no margins, so spacing should stay line height + extra size
            var themePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../Iguina.Demo/Assets/DefaultTheme"));
            _renderer = new ProportionalRenderer();
            _system = new UISystem(Path.Combine(themePath, "system_style.json"), _renderer, new ScriptedInputProvider());
            var list = AddItems(_system.Root.AddChild(new TestListBox(_system)), 30);
            list.SetVisibleItemsCount(6);
            Frames(3);
            Assert.That(list.PublicItemHeight, Is.EqualTo(30));
            AssertRowsSpacing(30);
            Assert.That(DrawnItems()[0].Y, Is.EqualTo(list.LastInternalBoundingRect.Top));
        }
    }
}
